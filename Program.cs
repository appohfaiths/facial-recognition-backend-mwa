using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Emgu.CV;
using Emgu.CV.Face;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using FacialRecognition.Data;
using FacialRecognition.Models;
using FacialRecognition.Seed;
using FacialRecognition.Service;
using System.Drawing;


var builder = WebApplication.CreateBuilder(args);

var connectionString = "Host=localhost;Port=5432;Username=kodjocode;Password=kodjocode1234;Database=facialrecognition";
string MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

builder.Services.AddCors(options => {
      options.AddPolicy(name: MyAllowSpecificOrigins,
                        builder => {
                           builder.WithOrigins("http://localhost:3000",
                                                "http://localhost:3001").AllowAnyHeader().AllowAnyMethod();
                        });
});
builder.Services.AddTransient<Seed>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddDbContext<FacialRecognitionContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddSwaggerGen(c =>
{
     c.SwaggerDoc("v1", new OpenApiInfo { Title = "FacialRecognition API", Description = "Let's build something great", Version = "v1" });
});
var app = builder.Build();

if (args.Length == 1 && args[0].ToLower() == "seeddata")
    SeedData(app);

void SeedData(IHost app)
{
    var scopedFactory = app.Services.GetService<IServiceScopeFactory>();

    using var scope = scopedFactory?.CreateScope();
    var service = scope?.ServiceProvider.GetService<Seed>();
    service?.SeedDataContext();
}

// Initialise Face Recognizer
var faceRecognizer = new EigenFaceRecognizer(80, double.PositiveInfinity);

app.UseCors(MyAllowSpecificOrigins);
app.UseSwagger();
app.UseSwaggerUI(c =>
{
   c.SwaggerEndpoint("/swagger/v1/swagger.json", "Facial Recognition API V1");
});


//Endpoints
app.MapGet("/", () => "Facial Recognition API v1.0.0");

// Saving the image to the database
app.MapGet("/photos", async (FacialRecognitionContext db) =>
{
   var photos = await db.Photos.ToListAsync();
   return photos;
});

app.MapPost("/photo", async (FacialRecognitionContext db, Photo photo) =>
{
   await db.Photos.AddAsync(photo);
   await db.SaveChangesAsync();
   return Results.Created($"/photo/{photo.Id}", photo);
});

app.MapGet("photo/{id}", async (FacialRecognitionContext db, int id) => await db.Photos.FindAsync(id));

app.MapDelete("/photo/{id}", async (FacialRecognitionContext db, int id) =>
{
    var photo = await db.Photos.FindAsync(id);
    if (photo is null)
        return Results.NotFound();
    db.Photos.Remove(photo);
    await db.SaveChangesAsync();
    return Results.Ok();
});

//save photos
app.MapPost("/upload-photo", async (HttpContext context, FacialRecognitionContext db) =>
{
    try
    {
        // Read the image data from the request body
        using (var memoryStream = new MemoryStream())
        {
            await context.Request.Body.CopyToAsync(memoryStream);
            byte[] photoData = memoryStream.ToArray();
            
            // Generate a unique name for the photo (e.g., using GUID)
            string photoName = Guid.NewGuid().ToString();
            
            // Store the photo in the database
            var photo = new Photo
            {
                Name = photoName,
                ImageData = photoData
            };
            db.Photos.Add(photo);
            await db.SaveChangesAsync();

            // Return the ID of the newly created photo
            return Results.Ok(new { photoId = photo.Id });
        }
    }
    catch (Exception ex)
    {
        return Results.BadRequest(ex.Message);
    }
});

// Recognizing faces
app.MapPost("/recognize", async (HttpContext context, FacialRecognitionContext db) =>
{
    try
    {
        // Read the JSON data from the request body
        using (var reader = new StreamReader(context.Request.Body))
        {
            var json = await reader.ReadToEndAsync();
            
            // Deserialize the JSON data into a class that represents your request
            var requestData = JsonConvert.DeserializeObject<RecognizedFace>(json);

            // Access the base64-encoded image data from the request data
            var imageBytes = requestData.FaceImageData;

            // Create a memory stream from the byte array
            using (var memoryStream = new MemoryStream(imageBytes))
            {
                // Detect and recognize faces in the uploaded image
                var image = new Mat();
                CvInvoke.Imdecode(memoryStream.ToArray(), ImreadModes.Color, image);

                var detectedFaces = FacialRecognitionService.DetectFaces(image);

                var recognizedFaces = new List<RecognizedFace>();

                foreach (var detectedFace in detectedFaces)
                {
                    // Recognize the face and get the label (person ID)
                    var label = FacialRecognitionService.RecognizeFace(detectedFace, faceRecognizer);

                    // Store the recognized face data in the database
                    var recognizedFace = new RecognizedFace
                    {
                        FaceImageData = detectedFace.ToImage<Bgr, byte>().Bytes,
                        Label = label,
                    };

                    db.RecognizedFaces.Add(recognizedFace);
                    recognizedFaces.Add(recognizedFace);
                }

                await db.SaveChangesAsync();

                return Results.Ok(recognizedFaces);
            }
        }
    }
    catch (Exception ex)
    {
        return Results.BadRequest(ex.Message);
    }
});

// Console.WriteLine(CvInvoke.BuildInformation);
app.UseHttpsRedirection();

app.Run();
