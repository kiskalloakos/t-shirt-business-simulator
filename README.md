# 3D Simulator

Unity project for building a small, responsive 3D simulator together.

## First launch

1. Open Unity Hub and sign in with a Unity account.
2. Install the current **Unity 6 LTS** editor with the macOS Build Support module.
3. In Hub, choose **Add** and select this folder.
4. Open the project. Unity generates its `Library/` folder on first launch.

## Project layout

- `Assets/Scenes` — playable levels and test scenes
- `Assets/Scripts` — gameplay and simulation code
- `Assets/Prefabs` — reusable world objects
- `Assets/Materials`, `Assets/Art` — visual assets
- `Assets/Settings` — renderer and gameplay settings

## Sensible Mac targets

Develop and profile at 1440×900 or 1080p. For a smooth laptop experience, start with the Universal Render Pipeline, baked lighting, modest shadow distances, and lightweight post-processing. The M4/16 GB configuration is well suited to an attractive stylized or simulation-focused 3D game; massive photoreal open worlds need more careful scope and optimization.

