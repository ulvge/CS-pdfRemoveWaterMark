param($installPath, $toolsPath, $package, $project)

function MarkDirectoryAsCopyToOutputRecursive($item)
{
    $item.ProjectItems | ForEach-Object { MarkFileASCopyToOutputDirectory($_) }
}

function MarkFileASCopyToOutputDirectory($item)
{
    Try
    {
        Write-Host Try set $item.Name
        $item.Properties.Item("CopyToOutputDirectory").Value = 2
    }
    Catch
    {
        Write-Host RecurseOn $item.Name
        MarkDirectoryAsCopyToOutputRecursive($item)
    }
}

#Now mark everything in the a directory as "Copy to newer"
MarkDirectoryAsCopyToOutputRecursive($project.ProjectItems.Item("x86"))
MarkDirectoryAsCopyToOutputRecursive($project.ProjectItems.Item("x64"))
MarkFileASCopyToOutputDirectory($project.ProjectItems.Item("icudt.dll"))
MarkFileASCopyToOutputDirectory($project.ProjectItems.Item("libpdfium.dylib"))

$DTE.ItemOperations.Navigate("https://patagames.com/API/install.ashx?packagePdf=pdfium&path=$installPath")
