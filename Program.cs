using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Emgu.CV.Face;
using FacialRecognition.Data;
using FacialRecognition.Models;
using FacialRecognition.Seed;


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
        var file = context.Request.Form.Files["photo"];

        if (file != null && file.Length > 0)
        {
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                byte[] photoData = memoryStream.ToArray();

                string photoName = context.Request.Form["photoName"];
                // Handle the case where "photoName" is missing, possibly by providing a default value
                photoName ??= "DefaultName";

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
        else
        {
            // Handle the case where no file was uploaded
            return Results.BadRequest("No file uploaded.");
        }
});


// Recognizing faces
app.MapPost("/recognize", async (HttpContext context, FacialRecognitionContext db) =>
{
      try
    {
        // Read the request body
        using (StreamReader reader = new StreamReader(context.Request.Body))
        {
            string requestBody = await reader.ReadToEndAsync();

            // Deserialize the JSON request body into your ImageDataModel
            RecognizedFace imageData = JsonConvert.DeserializeObject<RecognizedFace>(requestBody);

            // Perform the facial recognition logic using the imageData

            // Example response
            var response = new RecognizedFace
            {
                Id = 3,
                PhotoId = 2,
                Name = "Three",
                FaceImageData = new byte[0],
                Label = 3
            };

            // Save the recognized face to the database
            db.RecognizedFaces.Add(response);
            await db.SaveChangesAsync();

            return Results.Ok(imageData);
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
