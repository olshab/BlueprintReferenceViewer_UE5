namespace BlueprintReferenceViewer_UE5.Exceptions
{
    class ProjectNotFoundException : ApplicationException
    {
        public ProjectNotFoundException()
            : base("Project directory doesn't exist. Uncheck bScanProjectForReferencedAssets")
        { }
    }
}
