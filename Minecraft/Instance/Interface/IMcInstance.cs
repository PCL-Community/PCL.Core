using System;
using System.IO;
using System.Text.Json.Nodes;
using PCL.Core.App;
using PCL.Core.Minecraft.Instance.Handler;

namespace PCL.Core.Minecraft.Instance.Interface;

public interface IMcInstance : IMcInstanceBasic, IMcInstanceJson;