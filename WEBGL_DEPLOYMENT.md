# WebGL / GitHub Pages deployment

The repository includes a WebGL build command and a GitHub Pages workflow.
The playable URL will be:

`https://plumas90.github.io/Cosmic-Chaos-Cat/`

## One-time GitHub setup

1. Under **Settings > Pages > Build and deployment**, set **Source** to
   **GitHub Actions**.
2. Push this change to `main`, or run **Deploy WebGL to Pages** manually from
   the repository's **Actions** tab.

Because Unity Personal no longer supports manual license activation, WebGL is
built locally and the generated `docs` directory is committed with the source.
The workflow uploads that ready-made build and deploys it without Unity account
secrets.

## Local build

Install **WebGL Build Support** for Unity `6000.0.67f1` in Unity Hub. Then open
the project and select **Build > Cosmic Chaos Cat > WebGL**. Output is written
to `docs`. Commit the updated directory, then push it to `main`. Test it through
a local HTTP server; opening `index.html`
directly with a `file://` URL is not supported by browsers.

The build uses Brotli compression with Unity's decompression fallback. GitHub
Pages cannot set Unity's `Content-Encoding` response headers, so the fallback
lets the compressed build run without custom server configuration.
