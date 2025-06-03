using Anon.NET.Anonimization.Interfaces;

namespace Anon.NET.Anonimization.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class QuasiIdentifierAttribute : Attribute, IAnonymizationAttribute { }
