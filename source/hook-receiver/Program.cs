using System;using System.IO;
// A console subsystem executable makes both Windows shells wait for durable receipt.
// It inherits the hook runner's hidden console and emits no output.
public static class HookReceiverProgram {
 public static int Main(string[] args){try{int at=Array.IndexOf(args,"--hook-root");if(at>=0&&at+1<args.Length)return KianaPet.HookJournal.Receive(Path.GetFullPath(args[at+1]));}catch{}return 0;}
}
