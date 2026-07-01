global using System;
global using System.Buffers;
global using System.Collections;
global using System.Collections.Concurrent;
global using System.Collections.Generic;
global using System.Collections.ObjectModel;
global using System.ComponentModel;
global using System.Diagnostics;
global using System.Linq;
global using System.Net;
global using System.Net.NetworkInformation;
global using System.Reflection;
global using System.Runtime.CompilerServices;
global using System.Runtime.Versioning;
global using System.Net.Sockets;
global using System.Text;
global using System.Text.Json;
global using System.Text.Json.Serialization;
global using System.Threading.Tasks;


// Image Sharp 
global using SixLabors;
global using SixLabors.ImageSharp  ;
global using SixLabors.ImageSharp.Advanced;
global using SixLabors.ImageSharp.Formats;
global using SixLabors.ImageSharp.Formats.Bmp;
global using SixLabors.ImageSharp.Formats.Jpeg;
global using SixLabors.ImageSharp.Formats.Png;
global using SixLabors.ImageSharp.Metadata;
global using SixLabors.ImageSharp.Metadata.Profiles.Exif;
global using SixLabors.ImageSharp.PixelFormats;
global using SixLabors.ImageSharp.Processing;


// LibRaw 
global using Sdcb;
global using Sdcb.LibRaw;


// Openize HEIC 
// Conflicts with Image Sharp
// NOPE => global using Openize.Heic.Decoder;


// Framework 
global using Lyt.Framework.Interfaces.Localizing;
global using Lyt.Framework.Interfaces.Logging;
global using Lyt.Framework.Interfaces.Modeling;
global using Lyt.Framework.Interfaces.Messaging;
global using Lyt.Framework.Interfaces.Profiling;


// Model Utilities 
global using Lyt.Collections; 
global using Lyt.Model;
global using Lyt.Persistence;
global using Lyt.Utilities.Parallel;
global using Lyt.Utilities.Profiling;
global using Lyt.Utilities.Randomizing;


// Application Model 
global using Lyt.PhotoPostPro.Model;
global using Lyt.PhotoPostPro.Model.Algorithms;
global using Lyt.PhotoPostPro.Model.Frames;
global using Lyt.PhotoPostPro.Model.Messaging;
global using Lyt.PhotoPostPro.Model.PostProcessors;
global using Lyt.PhotoPostPro.Model.ProcessModels;
global using Lyt.PhotoPostPro.Model.ProjectModels;
global using Lyt.PhotoPostPro.Model.Utilities;

