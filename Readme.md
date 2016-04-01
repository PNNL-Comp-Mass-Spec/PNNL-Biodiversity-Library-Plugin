# PNNL Biodiversity Library Plugin

The Biodiversity Library Plugin is a Skyline plugin designed to allow pathway-centric browsing of peptides previously identified by LC-MS/MS. The plugin assists researchers in SRM assay design or DIA data analysis. Users select an organism and biological pathway of interest, then the plugin shows information about the associated peptides. 

In total the Biodiversity Library catalogs MS/MS spectra from over 118 distict organisms, including over 2 million peptides and 200,000 proteins. Users can optionally import peptide data for selected proteins into Skyline to extend their local copy of the Biodiversity Library.

## Database

File PBL.db is a SQLite database that the Biodiversity Library Plugin uses for tracking peptides, proteins, and pathways. Due to the size of the file it is not part of this repository. It is available on the panomics.pnl.gov website at <a href="http://panomics.pnnl.gov/data/">http://panomics.pnnl.gov/data</a>

## About Skyline

[Skyline](https://brendanx-uw1.gs.washington.edu/labkey/project/home/software/Skyline/begin.view) is an application for working with mass spectrometry based quantitative methods (SRM, MRM, DDA, etc.) and for analyzing the resulting mass spectrometer data. Learn more at the [MacCoss Lab Software](https://brendanx-uw1.gs.washington.edu/labkey/project/home/begin.view?) website.
