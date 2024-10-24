# SixPix.NET

Sixel image encoding and decoding library.

Namespace: `SixPix`

## Encoding ( :art: Image -> :ab: Sixiel string)

Encode to Sixel string from [SixLabors.ImageSharp]'s Image data.

### from Stream

#### Syntax:
```csharp
public static ReadOnlySpan<char> SixPix.Sixel.Encode(Stream stream)
```

#### Example:
```csharp
using SixPix;

using var fileStream = new FileStream(@"path/to/image.png", FileMode.Open);
ReadOnlySpan<char> sixelString = Sixel.Encode(fileStream);
Console.Out.WriteLine(sixelString);
```

### from Image

#### Syntax:
```csharp
public static ReadOnlySpan<char> SixPix.Sixel.Encode(Image<Rgb24> img)
```

#### Example:
```csharp
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixPix;

using Image<Rgb24> image = new Image<Rgb24>(width, height);
// drawing image ...
ReadOnlySpan<char> sixelString = Sixel.Encode(image);
Console.Out.WriteLine(sixelString);
```

## Decoding ( :ab: Sixel string -> :art: Image)

Decode to [SixLabors.ImageSharp]'s Image from Sixel string data.

### from Stream

#### Syntax:
```csharp
public static Image<Rgb24> Sixel.Decode(Stream stream)
```

#### Example:
```csharp
using SixLabors.ImageSharp.Formats.Png;
using SixPix;

using var fileStream = new FileStream(@"path/to/sixeldata", FileMode.Open);
using var image = Sixel.Decode(fs);
using var writeStream = new FileStream(@"path/to/sixel_image.png", FileMode.Create);
image.Save(writeStream, new PngEncoder());
```

### from string

#### Syntax:
```csharp
public static Image<Rgb24> Sixel.Decode(String sixelString)
```

#### Example:
```csharp
using SixLabors.ImageSharp.Formats.Png;
using SixPix;

var sixelString = "\x1bP7;1;q\"1;1;12;12"
                + "#0;2;100;0;0"
                + "#0!12~-"
                + "#0!12~"
                + "\x1b\\";
using Image<Rgb24> image = Sixel.Decode(sixelString);
using var writeStream = new FileStream(@"path/to/sixel_image.png", FileMode.Create);
image.Save(writeStream, new PngEncoder());
```

[SixLabors.ImageSharp]: https://github.com/SixLabors/ImageSharp
