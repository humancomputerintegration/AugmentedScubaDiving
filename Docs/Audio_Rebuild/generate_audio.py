from pathlib import Path
import hashlib, json, math, subprocess, uuid, wave
import numpy as np

PROJECT = Path(__file__).resolve().parents[2]
AUDIO = PROJECT / "Assets/Diving Simulation/Audio"
WORK = Path(__file__).resolve().parent
WORK.mkdir(parents=True, exist_ok=True)
NS = uuid.UUID("f20d10d2-9549-40f4-9d5e-4c1cf7af61ea")
manifest = {"validation": "Decoded signals measured only; not listened to or tested in Unity/Quest.", "assets": []}

def run(args):
    return subprocess.run(args, check=True, capture_output=True)

def probe(path):
    info = json.loads(run(["ffprobe", "-v", "error", "-show_entries", "stream=channels,sample_rate", "-of", "json", str(path)]).stdout)
    return info["streams"][0]

def measure(path):
    meta = probe(path)
    data = np.frombuffer(run(["ffmpeg", "-v", "error", "-i", str(path), "-f", "f32le", "pipe:1"]).stdout, dtype="<f4")
    peak = float(np.max(np.abs(data)))
    rms = float(np.sqrt(np.mean(data.astype("float64") ** 2)))
    return {"duration_s": round(len(data) / int(meta["sample_rate"]) / meta["channels"], 4), "channels": meta["channels"], "sample_rate": int(meta["sample_rate"]), "peak_dbfs": round(20 * math.log10(max(peak, 1e-12)), 3), "rms_dbfs": round(20 * math.log10(max(rms, 1e-12)), 3), "full_scale_samples": int(np.count_nonzero(np.abs(data) >= 1))}

def guid_for(path):
    return uuid.uuid5(NS, str(path.relative_to(PROJECT))).hex

def directory(path):
    path.mkdir(parents=True, exist_ok=True)
    meta = Path(str(path) + ".meta")
    if not meta.exists():
        meta.write_text("fileFormatVersion: 2\nguid: " + guid_for(path) + "\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n")

def record(path, purpose, guid_override=None, **more):
    guid = guid_override if guid_override else guid_for(path)
    Path(str(path) + ".meta").write_text("fileFormatVersion: 2\nguid: " + guid + "\nAudioImporter:\n  externalObjects: {}\n  serializedVersion: 8\n  defaultSettings:\n    serializedVersion: 2\n    loadType: 0\n    sampleRateSetting: 0\n    sampleRateOverride: 44100\n    compressionFormat: 0\n    quality: 1\n    conversionMode: 0\n    preloadAudioData: 1\n  platformSettingOverrides: {}\n  forceToMono: 0\n  normalize: 0\n  loadInBackground: 0\n  ambisonic: 0\n  3D: 0\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n")
    item = {"path": str(path.relative_to(PROJECT)), "guid": guid, "purpose": purpose, "sha256": hashlib.sha256(path.read_bytes()).hexdigest(), **measure(path), **more}
    manifest["assets"].append(item)
    return item

def pcm(path, samples, sr=44100):
    assert np.max(np.abs(samples)) < 1
    with wave.open(str(path), "wb") as handle:
        handle.setnchannels(1)
        handle.setsampwidth(2)
        handle.setframerate(sr)
        handle.writeframes((samples * 32767).astype("<i2").tobytes())

def tone(hz, seconds, amplitude=0.34):
    sr = 44100
    t = np.arange(round(seconds * sr)) / sr
    envelope = np.sin(np.pi * np.minimum(t / 0.01, 1) / 2) ** 2 * np.exp(-t * 8)
    envelope[-min(len(envelope), 300):] *= np.linspace(1, 0, min(len(envelope), 300))
    return amplitude * envelope * (0.8 * np.sin(2 * np.pi * hz * t) + 0.2 * np.sin(2 * np.pi * 2 * hz * t))

feedback = AUDIO / "CallFeedback"
directory(feedback)
gap = np.zeros(round(0.035 * 44100))
cues = {
    "Button_Click.wav": ("Short deterministic keypad click; generated sine signal, no sampled external audio.", tone(1350, 0.065, 0.28)),
    "Call_Connected.wav": ("Two rising confirmation notes for real connected state.", np.concatenate([tone(660, 0.15), gap, tone(990, 0.22)])),
    "Call_Ended.wav": ("Two falling confirmation notes for real ended state.", np.concatenate([tone(660, 0.14, 0.30), gap, tone(440, 0.19, 0.30)])),
}
for name, (purpose, samples) in cues.items():
    path = feedback / name
    pcm(path, samples)
    record(path, purpose)

recordings = AUDIO / "Elina"
directory(recordings)
elina_files = [
    ("Elina_answering_original.m4a", "Elina_answering.wav", "fea885d660925eed8abe47f879e32bdb"),
    ("Elina_calling_original.m4a", "Elina_calling.wav", "3eb62d3408b25d869dd850963990a5a3"),
]
for source_name, wav_name, preserved_guid in elina_files:
    original = PROJECT / "Docs/Audio_Sources" / source_name
    source_stats = measure(original)
    gain = min(-19.0 - source_stats["rms_dbfs"], -2.0 - source_stats["peak_dbfs"])
    path = recordings / wav_name
    run(["ffmpeg", "-v", "error", "-y", "-i", str(original), "-af", f"volume={gain:.4f}dB", "-c:a", "pcm_s16le", str(path)])
    record(path, "User-provided actual Elina recording; decoded to PCM WAV with constant gain only.", guid_override=preserved_guid, source=str(original.relative_to(PROJECT)), source_sha256=hashlib.sha256(original.read_bytes()).hexdigest(), source_signal=source_stats, gain_db=round(gain, 4), speech_synthesized=False, content_trimmed=False)

adjusted = AUDIO / "ElizaLevelAdjusted"
directory(adjusted)
for source_name in ["Eliza_answering.mp3", "Eliza_calling.mp3"]:
    original = AUDIO / source_name
    source_stats = measure(original)
    gain = min(-19.0 - source_stats["rms_dbfs"], -2.0 - source_stats["peak_dbfs"])
    path = adjusted / (original.stem + "_level_adjusted.wav")
    run(["ffmpeg", "-v", "error", "-y", "-i", str(original), "-af", f"volume={gain:.4f}dB", "-c:a", "pcm_s16le", str(path)])
    record(path, "Same original Eliza recording, decoded with constant volume gain only; original MP3 and metadata preserved.", source=str(original.relative_to(PROJECT)), source_sha256=hashlib.sha256(original.read_bytes()).hexdigest(), source_signal=source_stats, gain_db=round(gain, 4))

(WORK / "audio_manifest.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2))
for item in manifest["assets"]:
    print(item["path"], item["guid"], item["duration_s"], item["peak_dbfs"], item["rms_dbfs"])
