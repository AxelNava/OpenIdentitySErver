namespace OwnIdentity.LegacyInventory;

public static class Program
{
    public static int Main(string[] args)
    {
        return LegacyInventoryCli.Run(args, Console.Out, Console.Error);
    }
}
