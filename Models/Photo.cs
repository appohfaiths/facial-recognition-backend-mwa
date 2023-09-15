using System.ComponentModel.DataAnnotations;

namespace FacialRecognition.Models
{
    public class Photo
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required byte[] ImageData { get; set; } // Store the image as binary data

        public List<RecognizedFace> RecognizedFaces { get; set; }
    }
}