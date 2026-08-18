# Interactive XR Human Anatomy FYP Approach

## Project concept

**Working title:** Interactive XR Visualization of Selected 3D Human Anatomy Models with Basic Morphometric Analysis

Build an educational XR anatomy explorer with small, game-like learning activities. A learner can:

- view systems together or individually;
- select, label, highlight, isolate, and fade a structure;
- use a layer-peel control to move from skin to deeper structures;
- complete guided learning tasks and quizzes;
- inspect simple prepared measurements, such as length, surface area, volume, or ratios.

This is an **educational anatomy atlas with interaction**, not a simulation for diagnosis or treatment. Morphometric values describe the prepared reference model only; they must not be presented as patient-specific or diagnostic results.

## Scope decision

Do **not** build the complete human body layer by layer first. That creates a large, unfinished project with no complete learning experience.

Build **one region end to end**, then expand to the next region only after the first is stable. Every region should support the same complete interaction loop: layer controls, selection, labels, isolation, learning task, quiz, and measurement.

The first region should be the **lower limb, centred on the femur**. The femur is mandatory, and this region is a practical way to demonstrate all required systems without building the full body.

### Recommended FYP target

- **Primary region:** lower limb, hip to knee; optionally extend to lower leg only after the core works.
- **Systems:** skin/superficial layer, skeletal, muscular, major nerves, and major blood vessels.
- **Initial structure count:** 40-80 well-labelled structures; start with 12-20 in the first vertical slice.
- **Mandatory landmark:** femur, including useful landmarks such as head, neck, greater trochanter, shaft, medial/lateral condyles where supported by the source data.
- **Learning modes:** Explore, Guided Lesson, Quiz.
- **Measurements:** femur length, a selected muscle length, and one surface-area or volume example.
- **Platforms:** desktop XR simulator for daily work; real headset testing before final evaluation.

## Why region by region is the right priority

| Strategy | Result | Recommendation |
|---|---|---|
| Whole body, layer by layer | A skin layer, then a partial skeleton, then partial muscles; no area is ready to learn from and naming/optimisation work multiplies quickly. | Do not use as the primary plan. |
| Region by region | A complete, testable learning experience in one anatomical area; content and code can be reused for later areas. | Use this plan. |
| Femur-only prototype | Quickly proves selection, labels, measurements, and XR interaction, but is too narrow as the final product. | Use as the first milestone. |

The project should therefore grow like this:

```text
Femur prototype
  -> complete hip-to-knee lower-limb learning region
  -> optional lower-leg extension
  -> optional second region only if all core evaluation goals are complete
```

## Development priority

### Milestone 0 - academic and data foundation

1. Agree the learner level with the supervisor: high-school, foundation, or undergraduate anatomy.
2. Write measurable learning outcomes, for example: “identify the femur,” “distinguish quadriceps from hamstrings,” and “trace the femoral artery.”
3. Choose an anatomy dataset and record its licence, attribution, version, and permitted use.
4. Ask an anatomy lecturer or domain adviser to validate the selected structures, labels, facts, and measurement definitions.
5. Choose target hardware. Develop with the XR simulator, but arrange at least one real-headset usability test.

**Gate:** Do not create large amounts of content until the data source, learning outcomes, and target region are approved.

### Milestone 1 - femur vertical slice

Create a small but complete XR experience containing:

- one femur model at anatomically correct scale;
- selection ray/direct grab, highlight, label, and information panel;
- isolate and reset-view controls;
- a visible length measurement in millimetres;
- one short guided objective and one quiz question;
- desktop XR simulator testing.

**Gate:** A user can select the femur, understand what it is, complete a task, and see a correct prepared measurement.

### Milestone 2 - layer-peel proof

Add a small lower-limb stack around the femur:

- skin/superficial outer layer;
- selected thigh muscles;
- femur and nearby skeletal structures;
- femoral artery/vein and major nerves;
- layer slider, hide/show, transparency, and isolate controls.

Use visibility and transparency to create the peel effect. Do not spend time on physically simulated tearing skin.

**Gate:** The learner can move from skin to deeper layers without losing spatial context.

### Milestone 3 - complete lower-limb learning region

Expand the selected region to the planned 40-80 structures. Add grouped controls:

- **Skeletal:** femur first, then nearby pelvis/hip and knee structures as available.
- **Muscular:** quadriceps and hamstrings first; add major surrounding muscles only after labels and interactions work.
- **Nerves:** femoral and sciatic nerves first, then branches only where educationally necessary.
- **Vessels:** femoral artery and vein first, then selected major branches.
- **Skin/superficial:** one outer layer or clinically appropriate surface representation.

Do not make every microscopic landmark independently interactive unless it supports a stated learning outcome.

**Gate:** Every included structure has a correct name, system, parent group, label position, source reference, and acceptable performance.

### Milestone 4 - educational game loop

Add learning value after exploration is reliable:

- guided “find this structure” tasks;
- multiple-choice and direct-selection quizzes;
- progressive difficulty or a simple score;
- progress feedback;
- an accessible non-VR/desktop fallback if time allows.

**Gate:** The app demonstrates learning, not only impressive graphics.

### Milestone 5 - morphometric analysis and evaluation

Implement only measurements that can be explained and verified:

- femur length: fixed anatomical landmark to landmark;
- muscle length: documented start/end reference points;
- surface area or volume: calculated from a closed, correctly scaled mesh or stored as verified reference data;
- optional ratios: clearly label the formula and units.

Record model scale at import. Use a consistent convention such as Unity units in metres while displaying medical values in millimetres.

Evaluate with users and the anatomy adviser: task completion, quiz result, usability issues, frame rate, and perceived learning value.

## Anatomy content pipeline

Use anatomically sourced data rather than AI-generated anatomy meshes.

```text
Licensed/open anatomy source model
  -> source and licence record
  -> Blender / 3D Slicer cleanup and separation
  -> correct scale, pivot, naming, materials, LODs
  -> anatomy metadata record for every structure
  -> Unity import and XR interaction setup
  -> lecturer/domain review
```

Potential starting sources include Z-Anatomy, Open Anatomy atlases, and the NIH Visible Human Project. Check the exact terms for the particular asset before use; do not assume all related assets share one licence.

### Structure metadata

Every interactive mesh needs a consistent record, stored later in JSON, CSV, or Unity ScriptableObjects:

```text
anatomyId: femur
displayName: Femur
system: Skeletal
parentGroup: LowerLimb
laterality: Left | Right | Midline
mesh: Femur_LOD1
sourceAsset: [dataset and version]
labelAnchor: [Transform]
measurementUnits: mm
learningFacts: [...]
```

This metadata is what lets one structure be selected alone while also being shown with its full system.

## Tool choices

| Job | Recommended tool | Role |
|---|---|---|
| Project planning, Unity C#, data schema, editor tools, debugging | Codex | Build and maintain the implementation; generate focused scripts, not unverified anatomy facts. |
| XR interaction | Unity 6, OpenXR, XR Interaction Toolkit | Ray/direct interaction, grab, locomotion, simulator, and headset support. |
| Anatomy source models | Validated open/licensed anatomical dataset | The source of anatomical geometry and labels. |
| Mesh cleanup, naming, LODs, pivot/scale, materials | Blender | Prepare source meshes for reliable real-time use. |
| Medical image segmentation, only if needed | 3D Slicer | Work from CT/MRI-style source data; not required for the first prototype. |
| UI concepts and non-medical art | Image AI | Create visual references, icons, and UI drafts only. |
| Version control for large assets | Git + Git LFS | Store code, scenes, metadata, and large model files safely. |

### Do not use generative 3D models for

- femur or other anatomical teaching structures;
- nerves, blood vessels, muscles, or organ placement;
- any object used for a morphometric claim;
- labels or facts without human academic review.

Meshy and Tripo remain useful for non-medical environmental assets, but not as the anatomy authority.

## Unity architecture plan

```text
AnatomyRoot
  -> LowerLimb
      -> SkinGroup
      -> SkeletalGroup
          -> Femur
      -> MuscularGroup
      -> NerveGroup
      -> VesselGroup

XR Rig
UI / Learning Manager
Anatomy Layer Manager
Selection and Highlight Manager
Measurement Manager
Quiz Manager
```

The layer manager controls show/hide, transparency, and isolate modes. The selection manager reads each object's anatomy metadata to show labels and facts. The measurement manager should use fixed, documented landmark transforms rather than arbitrary pointer clicks for assessed values.

## Performance and quality rules

- Keep a high-quality source copy outside the runtime scene.
- Create optimised runtime copies with sensible mesh LODs and texture sizes.
- Load later anatomical regions only when required; use Addressables if the project grows.
- Avoid thousands of separately active meshes in the first build.
- Profile early on the intended headset class, not only in the Unity editor.
- Keep a data-source and academic-review record for every included structure.
- Test the project in the XR simulator daily and on a real headset before final evaluation.

## Definition of a successful FYP

The project is successful if a learner can use XR controls to explore a lower-limb anatomy model, peel through the selected systems, identify the femur and selected supporting structures, isolate them, complete guided/quiz tasks, and inspect clear, correctly defined basic measurements.

It does not need to recreate a commercial full-body atlas containing thousands of structures.

## Relation to the existing workflow

See [AI_UNITY_GAME_WORKFLOW.md](AI_UNITY_GAME_WORKFLOW.md) for the general AI-first Unity workflow. This document overrides its general “generate 3D props” recommendation for anatomy: clinical/educational anatomy must begin with a verified source dataset and human domain review.
