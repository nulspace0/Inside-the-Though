VR educational game about the human brain. Unity 6.
# Inside the Thought — VR Brain Education Game
https://youtu.be/2R2fYcBn5U8

A PC VR educational game for Meta Quest 3 (via Link/Air Link) 
and HTC Vive Cosmos Pro. Players explore the human brain from 
the inside. Each brain lobe is a separate interactive scene 
with facts, puzzles, and visual effects.

## Gameplay
- Select a brain lobe on the main scene
- Pop balloons with facts about the lobe
- Connect synapses to unlock new objects
- Read text labels by entering trigger zones

## Tech Stack
- Unity 6 · C#
- XR Interaction Toolkit 3.2
- Meta Quest 3 / HTC Vive Cosmos Pro
- TextMesh Pro · Rigidbody physics

## Architecture highlights
- `VRPauseMenu` — procedurally built VR menu, tracks camera every frame
- `WireConnector` — synapse connection via Rigidbody joints and triggers
- `RespawnManager` — singleton, raycast-based nearest platform search
- `DistanceFadeTextUI` — text fade using BoxCollider trigger zone

## Third-party assets used
- [Cartoon FX Remaster](https://assetstore.unity.com/packages/vfx/particles/cartoon-fx-remaster-free-109565) — JMO Assets
- [Runes and Portals](https://assetstore.unity.com/packages/vfx/shaders/runes-portals-195246) — PaulosCreations

## Documentation
[....](....)
