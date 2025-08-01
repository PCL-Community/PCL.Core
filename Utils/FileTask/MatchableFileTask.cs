using System.Collections.Generic;
using PCL.Core.Model.Files;

namespace PCL.Core.Utils.FileTask;

public class MatchableFileTask : FileTask
{
    public List<FileMatchPair<FileTransfer>> MatchTransfers { get; } = [];
    public List<FileMatchPair<FileProcess>> MatchProcesses { get; } = [];

    public MatchableFileTask(
        FileItem item,
        FileMatchPair<FileTransfer>? transfer = null,
        FileMatchPair<FileProcess>? process = null
    ) : base(item)
    {
        if (transfer != null) MatchTransfers.Add(transfer);
        if (process != null) MatchProcesses.Add(process);
    }

    public MatchableFileTask(
        IEnumerable<FileItem> items,
        IEnumerable<FileMatchPair<FileTransfer>>? transfers = null,
        IEnumerable<FileMatchPair<FileProcess>>? processes = null
    ) : base(items)
    {
        if (transfers != null) MatchTransfers.AddRange(transfers);
        if (processes != null) MatchProcesses.AddRange(processes);
    }

    public override FileProcess? GetProcess(FileItem item) => MatchProcesses.MatchFirst(item);
    public override IEnumerable<FileTransfer> GetTransfer(FileItem item) => MatchTransfers.MatchAll(item);
}
