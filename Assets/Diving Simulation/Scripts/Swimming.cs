using System.Collections.Generic;
using UnityEngine;

public class Swimming : MonoBehaviour
{
    [Range(0f, 1f)]
    [SerializeField] float forwardAmount = 1f;

    Animator animator;
    readonly HashSet<string> parameters = new HashSet<string>();

    void Start()
    {
        animator = GetComponent<Animator>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!animator)
        {
            Debug.LogWarning("SwimmerAnimation: no Animator found on " + name, this);
            enabled = false;
            return;
        }

        animator.applyRootMotion = false;

        foreach (AnimatorControllerParameter parameter in animator.parameters) parameters.Add(parameter.name);
    }

    void Update()
    {
        SetBool("IsSwimming", true);
        SetBool("OnGround", false);
        SetBool("Crouch", false);
        SetFloat("Forward", forwardAmount);
        SetFloat("Turn", 0f);
    }

    void SetBool(string name, bool value)
    {
        if (parameters.Contains(name)) animator.SetBool(name, value);
    }

    void SetFloat(string name, float value)
    {
        if (parameters.Contains(name)) animator.SetFloat(name, value);
    }
}