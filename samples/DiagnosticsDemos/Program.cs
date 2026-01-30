// DiagnosticsDemos - Sample project demonstrating ErrorOrX diagnostics
//
// This project exists to demonstrate all ErrorOrX analyzer and generator diagnostics.
// It is not intended to be run as an actual web application.
//
// To view diagnostics:
// 1. Open in your IDE (Visual Studio, Rider, VS Code)
// 2. Navigate to files in the Demos/ folder
// 3. Uncomment the "TRIGGERS" code sections to see diagnostics

var builder = WebApplication.CreateSlimBuilder(args);
var app = builder.Build();

// Map all ErrorOr endpoints
app.MapErrorOrEndpoints();

app.Run();
