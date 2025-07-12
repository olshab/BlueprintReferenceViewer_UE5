namespace BlueprintReferenceViewer_UE5
{
    static class Program
    {
        static void Main()
        {
            ViewerSettings Settings = new()
            {
                DumpFolder = @"C:\Users\Oleg\Desktop\BlueprintReferenceViewer",
                DumpList = @"C:\Users\Oleg\Desktop\ToDump.txt",
                //PaksDirectory = @"C:\Program Files (x86)\Steam\steamapps\common\Dead by Daylight\DeadByDaylight\Content\Paks",
                //EngineVersion = "GAME_DeadByDaylight",
                PaksDirectory = @"E:\3.0.0\DeadByDaylight\Content\Paks",
                EngineVersion = "GAME_UE4_21",
                MappingsFilepath = @"C:\Users\Oleg\Mappings.usmap",
                AESKey = "0x22B1639B548124925CF7B9CBAA09F9AC295FCF0324586D6B37EE1D42670B39B3",
                
                bScanProjectForReferencedAssets = true,
                ProjectDirectory = @"C:\Users\Oleg\Desktop\DBDOldTiles",
                AssetsPackagePathToScanAt = "/Game/OriginalTiles",

                bSearchForActorSpawners = false,
                bIgnoreEditorOnlyVisualization = true,
            };
            
            ReferenceViewer Viewer = new(Settings);
            Viewer.Execute();
        }
    }
}
