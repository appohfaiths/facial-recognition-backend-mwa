
namespace FacialRecognition.Models
{
    public class RecognizedFace
    {
        public int Id { get; set; }
        public int PhotoId { get; set; }
        public string Name { get; set; }
        public byte[] FaceImageData { get; set; } 
        public int Label { get; set; }

        public Photo Photo { get; set; }
    }
}