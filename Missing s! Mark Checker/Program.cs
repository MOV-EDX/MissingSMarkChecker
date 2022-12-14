using Microsoft.Win32;
using Missing_s__Mark_Checker;

string[] arguments = Environment.GetCommandLineArgs();
string path = string.Empty;

Console.Write("Enter the name to load COMPONENTS hive as: ");
string hive_name = Console.ReadLine();

if (arguments.Length < 2)
{
    Console.Write("Enter the path of the COMPONENTS hive to load: ");
    path = Console.ReadLine();
}
else
{
    //Grab the path from the dropped file
    path = arguments[1];
}

RegistryKey deployments = null;

try
{
    HiveLoader.GrantPrivileges();
    int result = HiveLoader.LoadHive(path, hive_name);
    HiveLoader.RevokePrivileges();

    if (result != 0)
    {
        Console.WriteLine("Unable to load the registy hive");
        return;
    }

    Console.WriteLine("Reading Deployments subkey, please wait...");

    deployments = HiveLoader.HKLM.OpenSubKey($@"{hive_name}\CanonicalData\Deployments", true);

    foreach (string deployment_name in deployments.GetSubKeyNames())
    {
        RegistryKey deployment = deployments.OpenSubKey(deployment_name, true);

        int smark_count = deployment.GetValueNames().Where(v => v.StartsWith("s!")).Count();
        int pmark_count = deployment.GetValueNames().Where(v => v.StartsWith("p!")).Count();

        if (pmark_count > smark_count)
        {
            foreach (string p_mark in deployment.GetValueNames().Where(v => v.StartsWith("p!")))
            {
                object value = deployment.GetValue(p_mark);
                RegistryValueKind data_type = deployment.GetValueKind(p_mark);
                string s_mark = p_mark.Replace("p!", "s!");

                deployment.SetValue(s_mark, value, data_type);
            }
        }

        deployment.Close();
    }

    Console.WriteLine("The repair(s) - if any - have been completed...");
}
catch (Exception e)
{
    Console.WriteLine($"An exception has occured: {e.Message}");
}
finally
{
    HiveLoader.GrantPrivileges();
    
    if (deployments is not null) deployments.Close();
    int result = HiveLoader.UnloadHive(hive_name);
    HiveLoader.HKLM.Close();

    HiveLoader.RevokePrivileges();

    if (result == 0) Console.WriteLine("Successfully unloaded hive");

    Console.WriteLine("Please press any key to exit...");
    Console.ReadKey();
}
