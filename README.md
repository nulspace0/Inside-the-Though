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
- Read text labels by entering trigger zones <img width="1541" height="1021" alt="заставка" src="https://github.com/user-attachments/assets/fa96947e-0951-4ab2-aabd-7558d7e4e89f" />

## Tech Stack
- Unity 6 · C#
- XR Interaction Toolkit 3.2
- Meta Quest 3 / HTC Vive Cosmos Pro
- TextMesh Pro · Rigidbody physics
 <img width="1383" height="1137" alt="1" src="https://github.com/user-attachments/assets/524fcf33-fc15-4042-b082-8a95e5451f76" />

## Architecture highlights
- `VRPauseMenu` — procedurally built VR menu, tracks camera every frame
- `WireConnector` — synapse connection via Rigidbody joints and triggers
- `RespawnManager` — singleton, raycast-based nearest platform search
- `DistanceFadeTextUI` — text fade using BoxCollider trigger zone
  <img width="1052" height="717" alt="4" src="https://github.com/user-attachments/assets/725b8225-93e3-4d8e-86e3-910e239673cc" />

## Third-party assets used
- [Cartoon FX Remaster](https://assetstore.unity.com/packages/vfx/particles/cartoon-fx-remaster-free-109565) — JMO Assets
- [Runes and Portals](https://assetstore.unity.com/packages/vfx/shaders/runes-portals-195246) — PaulosCreations
<img width="1042" height="840" alt="5" src="https://github.com/user-attachments/assets/29f111bf-70bf-4d32-8804-d5e72595f95a" />
<img width="988" height="955" alt="7" src="https://github.com/user-attachments/assets/62037e12-4130-484b-b827-ed5b774f77d7" />

