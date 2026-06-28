using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StudentPortal.Api.Services.Interfaces;

public class FaceMatch
{
    public string StudentKey { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public int[] Box { get; set; } = Array.Empty<int>();
}

public class FaceRecognitionResponse
{
    public List<FaceMatch> Matches { get; set; } = new();
}

public interface IFaceRecognitionClient
{
    Task<FaceRecognitionResponse> RecognizeAsync(string base64Image);
}
