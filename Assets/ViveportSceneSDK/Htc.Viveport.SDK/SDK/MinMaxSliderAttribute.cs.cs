
using System;
using UnityEngine;

/// <summary>
/// Taken from this gist: https://gist.github.com/frarees/9791517
/// </summary>
public class MinMaxSliderAttribute : PropertyAttribute
{

    public readonly float max;
    public readonly float min;

    public MinMaxSliderAttribute(float min, float max)
    {
        this.min = min;
        this.max = max;
    }
}