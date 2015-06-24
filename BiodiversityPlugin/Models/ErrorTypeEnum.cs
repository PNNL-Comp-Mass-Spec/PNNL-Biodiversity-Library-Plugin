namespace BiodiversityPlugin.Models
{
    public enum ErrorTypeEnum
    {
        SkylineError, // Skyline version issue
        NcbiError, // NCBI connection issue (getting the FASTAs)
        MassiveError, // Spectral Library Issue (downloading the .blibs)
        None // Default, should never be this
    }
}
