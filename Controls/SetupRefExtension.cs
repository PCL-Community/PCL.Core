using System;
using System.Windows.Markup;
using PCL.Core.ProgramSetup;
using PCL.Core.Service;

namespace PCL.Core.Controls;

[MarkupExtensionReturnType(typeof(ISetupEntry))]
public class SetupRefExtension() : MarkupExtension
{
    public string? Target { get; set; }

    public SetupRefExtension(string target) : this()
    {
        Target = target;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (Target is null)
            throw new ArgumentException("未指定 SetupRefExtension.Target 的值");
        return SetupService.Setup.GetEntryFromPath(Target) ?? throw new ArgumentException("未找到配置项 " + Target);
    }
}