﻿using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace PCL.Core.Minecraft.Instance.Interface;

public interface IJsonBasedInstance {
    JsonObject? VersionJson { get; }

    JsonObject? VersionJsonInJar { get; }
}
