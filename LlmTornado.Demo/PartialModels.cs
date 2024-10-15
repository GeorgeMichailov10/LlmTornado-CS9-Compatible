using System;
using System.AttributeUsage; 
using System.AttributeTargets;

namespace LlmTornado.Demo
{

[AttributeUsage(AttributeTargets.Field)]
public class FlakyAttribute : Attribute
{
    public string? Reason { get; set; }

    public FlakyAttribute(string? reason = null)
    {
        Reason = reason;
    }
}
}