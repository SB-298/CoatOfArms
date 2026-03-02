# Generates 3-colour background pattern mask PNGs for Coat of Arms mod.
# Mask convention: R = primary, G = secondary, B = tertiary (0-255).
# Run from repo root. Requires .NET / System.Drawing (Windows).

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

$size = 128
$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$outDir = Join-Path $root "Textures\CoatOfArms\Backgrounds"
if (-not (Test-Path $outDir)) {
    New-Item -ItemType Directory -Path $outDir -Force | Out-Null
}

function SetPixel($bmp, $x, $y, $r, $g, $b) {
    $bmp.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(255, $r, $g, $b))
}

function SaveMask($name, $scriptBlock) {
    $bmp = New-Object System.Drawing.Bitmap($size, $size)
    try {
        & $scriptBlock $bmp
        $path = Join-Path $outDir "$name.png"
        $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
        Write-Host "Wrote $path"
    } finally {
        $bmp.Dispose()
    }
}

# Vertical thirds: left = R, middle = G, right = B
SaveMask "VerticalThirdsThree" {
    param($bmp)
    for ($y = 0; $y -lt $size; $y++) {
        for ($x = 0; $x -lt $size; $x++) {
            if ($x -lt $size/3) { SetPixel $bmp $x $y 255 0 0 }
            elseif ($x -lt 2*$size/3) { SetPixel $bmp $x $y 0 255 0 }
            else { SetPixel $bmp $x $y 0 0 255 }
        }
    }
}

# Horizontal thirds: top = R, middle = G, bottom = B
SaveMask "HorizontalThirdsThree" {
    param($bmp)
    for ($y = 0; $y -lt $size; $y++) {
        for ($x = 0; $x -lt $size; $x++) {
            if ($y -lt $size/3) { SetPixel $bmp $x $y 255 0 0 }
            elseif ($y -lt 2*$size/3) { SetPixel $bmp $x $y 0 255 0 }
            else { SetPixel $bmp $x $y 0 0 255 }
        }
    }
}

# Quarters: top-left R, top-right G, bottom-left G, bottom-right B (classic 3-colour quartering)
SaveMask "QuartersThree" {
    param($bmp)
    $midX = [int]($size/2)
    $midY = [int]($size/2)
    for ($y = 0; $y -lt $size; $y++) {
        for ($x = 0; $x -lt $size; $x++) {
            if ($y -lt $midY) {
                if ($x -lt $midX) { SetPixel $bmp $x $y 255 0 0 } else { SetPixel $bmp $x $y 0 255 0 }
            } else {
                if ($x -lt $midX) { SetPixel $bmp $x $y 0 255 0 } else { SetPixel $bmp $x $y 0 0 255 }
            }
        }
    }
}

# Diagonal thirds: band from top-left to bottom-right split into three
SaveMask "DiagonalThree" {
    param($bmp)
    for ($y = 0; $y -lt $size; $y++) {
        for ($x = 0; $x -lt $size; $x++) {
            $d = $x + $y
            $max = 2 * ($size - 1)
            if ($d -lt $max/3) { SetPixel $bmp $x $y 255 0 0 }
            elseif ($d -lt 2*$max/3) { SetPixel $bmp $x $y 0 255 0 }
            else { SetPixel $bmp $x $y 0 0 255 }
        }
    }
}

# Checkers 3-colour: 4x4 grid repeating R, G, B
SaveMask "CheckersThree" {
    param($bmp)
    $block = [int]($size/4)
    $cols = @(255,0,0), (0,255,0), (0,0,255)
    for ($y = 0; $y -lt $size; $y++) {
        for ($x = 0; $x -lt $size; $x++) {
            $ix = [int][Math]::Floor($x / $block) % 3
            $iy = [int][Math]::Floor($y / $block) % 3
            $idx = ($ix + $iy) % 3
            $c = $cols[$idx]
            SetPixel $bmp $x $y $c[0] $c[1] $c[2]
        }
    }
}

# Bend three: diagonal bands (bend = stripe top-left to bottom-right), three colours
SaveMask "BendThree" {
    param($bmp)
    for ($y = 0; $y -lt $size; $y++) {
        for ($x = 0; $x -lt $size; $x++) {
            $d = $x - $y + $size
            $max = 2 * $size
            if ($d -lt $max/3) { SetPixel $bmp $x $y 255 0 0 }
            elseif ($d -lt 2*$max/3) { SetPixel $bmp $x $y 0 255 0 }
            else { SetPixel $bmp $x $y 0 0 255 }
        }
    }
}

Write-Host "Done. Masks saved to $outDir"
