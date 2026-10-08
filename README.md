# Youssef Moataz Portfolio

Full stack portfolio built with ASP.NET Core 10 and a responsive English frontend.

## Run locally

```powershell
dotnet run --project .\portfolio.csproj --urls http://localhost:5197
```

Open `http://localhost:5197` in a browser. The server provides the portfolio frontend and these endpoints:

- `GET /api/health` reports whether the backend is running.
- `GET /api/projects` returns the project cards shown on the page.
- `POST /api/contact` validates and stores contact messages in `data/contact-messages.json`.

Set `PORTFOLIO_DATA_DIR` to a persistent writable directory when deploying the app. Contact submissions are saved there; email delivery is not configured.

## Deploy on Render

This repository includes a `Dockerfile` and `render.yaml` for an ASP.NET Core web service.

1. Push the project to a GitHub repository.
2. In Render, choose **New > Blueprint** and connect that repository.
3. Review the `youssef-moataz-portfolio` service and deploy it.
4. When the first deploy completes, open the generated `onrender.com` URL.

The free service uses temporary local storage, so contact form submissions are not retained reliably after restarts or redeploys. Email delivery is not configured.

## Personalize

- Replace the sample project concepts in `Program.cs` with real project details and links.
- Replace `wwwroot/profile.jpeg` with an updated profile photo if needed.
- Update the LinkedIn and email links in `wwwroot/index.html`.
- Update the skill list and biography to match your experience.
