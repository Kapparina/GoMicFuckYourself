$msiInstallStateAbsent = 2 # Absent state
$upgradeCode = '{7E383A7A-9580-48A6-818E-B173FEE980C8}'
$installer = New-Object -ComObject WindowsInstaller.Installer

$products = $installer.RelatedProducts($upgradeCode)

if ($products.Count -eq 0)
{
    Write-Host "No products found with the specified upgrade code."
    exit
}
Write-Host "Found $($products.Count) products related to upgrade code: $upgradeCode"
foreach ($product in $products)
{
    Write-Host "Product Code: $product"
    try
    {
        $installer.ConfigureProduct($product, 0, $msiInstallStateAbsent)
        Write-Host "Uninstallation initiated for product: $product"
    }
    catch
    {
        Write-Host "Error configuring product: $_"
    }
}

[System.Runtime.Interopservices.Marshal]::ReleaseComObject($installer) | Out-Null
Write-Host "Uninstallation process completed."