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
public static ReadOnlySpan<char> SixPix.Sixel.Encode(Image<Rgba32> img)
```

#### Example:
```csharp
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixPix;

using var image = new Image<Rgba32>(width, height);
// drawing image ...
ReadOnlySpan<char> sixelString = Sixel.Encode(image);
Console.Out.WriteLine(sixelString);
```

### Create encoder instance and encode

#### Syntax:
```csharp
public static SixelEncoder Sixel.CreateEncoder(Image<Rgba32> image)
public static SixelEncoder Sixel.CreateEncoder(string path)
public static SixelEncoder Sixel.CreateEncoder(Stream stream)
```

#### Example:
```csharp
using SixPix;

using var encoder = Sixel.CreateEncoder(@"path/to/image.png");
encoder.Resize(widht: 200, height: 200);

// Encode to Sixel string the automatically choosed frame, normaly the first frame (index = 0).
string sixelString1 = encoder.Encode();

// Encode to Sixel string frame of the index
string sixelString2 = encoder.EncodeFrame(1);

// Enumerate encoded string
foreach (var sixelString in encoder.EncodeFrames())
{
    // ...
}

// Animation
using var ct = new CancellationTokenSource(10 * 1000); // Stop aflter 10 seconds
var animationTask = encoder.Animate(ct.Token);
animationTask.Wait();
```

## Decoding ( :ab: Sixel string -> :art: Image)

Decode to [SixLabors.ImageSharp]'s Image from Sixel string data.

### from Stream

#### Syntax:
```csharp
public static Image<Rgba32> Sixel.Decode(Stream stream)
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
public static Image<Rgba32> Sixel.Decode(String sixelString)
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
using var image = Sixel.Decode(sixelString);
using var writeStream = new FileStream(@"path/to/sixel_image.png", FileMode.Create);
image.Save(writeStream, new PngEncoder());
```

[SixLabors.ImageSharp]: https://github.com/SixLabors/ImageSharp
