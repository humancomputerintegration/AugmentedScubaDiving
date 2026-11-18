# Elina 실제 녹음 원본과 Unity 변환본

사용자가 제공한 실제 Elina 녹음 두 개를 사용합니다. 이 폴더의 M4A는 업로드한 원본과 바이트 단위로 동일합니다. Unity에서 재생하는 WAV는 프로젝트의 `Assets/Diving Simulation/Audio/Elina/`에 있습니다.

| 원본 보존 파일 | Unity 재생 파일 | 재생되는 상황 |
|---|---|---|
| Elina_answering_original.m4a | Elina_answering.wav | 플레이어가 Elina에게 발신했을 때 응답 |
| Elina_calling_original.m4a | Elina_calling.wav | Elina에게 온 수신 전화를 플레이어가 받았을 때 |

## 어떤 변환을 했나요?

원본은 48kHz mono AAC가 담긴 M4A입니다. FFmpeg로 48kHz mono PCM 16-bit WAV를 만들었습니다. 디코딩한 샘플의 개수와 순서를 유지했고, 일정 음량만 조정했습니다.

| 항목 | 응답 녹음 | 수신용 녹음 |
|---|---:|---:|
| 디코딩 후 길이 | 4.3307초 | 4.3093초 |
| 원본 피크 | -0.043dBFS | -0.363dBFS |
| 원본 RMS | -19.052dBFS | -19.891dBFS |
| 적용한 일정 gain | -1.957dB | -1.637dB |
| WAV 피크 | -2.001dBFS | -2.000dBFS |
| WAV RMS | -21.009dBFS | -21.528dBFS |
| 샘플 수 | 207,872 | 206,848 |

dBFS의 0은 디지털 신호의 최대치입니다. 원본은 이미 충분한 신호 크기를 갖고 있어 조금 줄여 피크 여유를 확보했습니다. RMS는 파일 전체 신호 크기의 측정값입니다. 실제 말소리의 체감 크기는 Quest에서 다른 주변음과 함께 확인해야 합니다.

일정 gain을 적용한 원본 디코딩 값과 WAV를 비교한 상관계수는 두 파일 모두 0.99999999 이상이었습니다. 최대 샘플 오차는 약 0.00001528로 PCM 16-bit 양자화 수준이며, 샘플 수·샘플레이트·채널 수는 같습니다. 0dBFS 이상 샘플은 두 WAV 모두 0개였습니다.

## 기존 통화 연결이 유지되는 이유

Scene은 파일 이름 대신 AudioClip의 GUID를 참조합니다. 응답 GUID `fea885d660925eed8abe47f879e32bdb`, 수신용 GUID `3eb62d3408b25d869dd850963990a5a3`를 새 WAV의 메타데이터에 그대로 이어받았습니다. 두 GUID는 Assets 전체에서 각각 한 번씩만 존재합니다. 이전 임시 음원은 제거했습니다.

원본 해시:

- Elina_answering_original.m4a: `db69da361a14faf40a86bb0be1c8c1eb6c02fb5a27fa0dbb017dcb7462e5bf13`
- Elina_calling_original.m4a: `0b0d9f3b95193cf43a114bd23c257e1f6dcef111be2c717e984a0851e042f1ad`

자세한 파일 측정값은 `../Call_Audio_Manifest.json`, 원본 동일성·파형·GUID 검사는 `../Elina_Recording_Validation.json`에 있습니다. 파일 디코딩·수치 검사를 수행했으며 Unity 또는 Quest에서 실제 재생한 결과는 아닙니다.
