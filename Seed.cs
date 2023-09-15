using Microsoft.EntityFrameworkCore;
using FacialRecognition.Models;
using FacialRecognition.Data;
using System.IO;

namespace FacialRecognition.Seed
{
    public class Seed 
    {
        private readonly FacialRecognitionContext facialRecognitionContext;
        public Seed(FacialRecognitionContext facialRecognitionContext)
        {
            this.facialRecognitionContext = facialRecognitionContext;
        }
        public void SeedDataContext()
        {
            if(!facialRecognitionContext.Photos.Any())
            {
                var photos = new[]
                {
                    "./SeedImages/FAITH.jpg",
                    "./SeedImages/FSA.jpg"
                };

                foreach (var photo in photos)
                {
                    var photoName = Path.GetFileNameWithoutExtension(photo);
                    var photoData = File.ReadAllBytes(photo);
                    var newPhoto = new Photo
                    {
                        Name = photoName,
                        ImageData = photoData
                    };

                    facialRecognitionContext.Photos.Add(newPhoto);
                }
                facialRecognitionContext.SaveChanges();
            }
        }
    }
}