# Project Agent Instructions

## Card image mapping rule

When a card image under `Assets/image/A_TEST` is approved for catalog mapping:

1. Treat `A_TEST` as a staging area only. Do not leave a production catalog or editor sync reference pointing into `A_TEST`.
2. Assign the next card ID and move the image into a suitably named folder under `Assets/image/A_No`, following the existing `<ID>_<card_name>` convention.
3. Move the image and its existing `.meta` file together. Never recreate the image `.meta`, because its GUID and sprite-slice file IDs are already referenced by Unity assets.
4. Add and commit the destination folder's `.meta` file as well.
5. Point both `CardCatalog.asset` and `CreateOrSyncCardCatalog.cs` at the finalized asset under `A_No`.
6. For a multi-sprite sheet, use the first valid sprite slice as the default `CardSprite`, then map only valid slices in their intended order.
7. After mapping, verify that no production reference to the moved source remains under `A_TEST`, the card ID is unique, and `git diff --check` passes.

If a card has special behavior such as click cycling, no evolution, breakthrough stages, or probabilistic effects, preserve that behavior explicitly in both runtime logic and the catalog-sync rule.

## Default breakthrough sprite distribution

Use this rule only when the user supplies multiple evolution images without specifying their breakthrough stages or a special click/animation behavior. Explicit card-specific instructions always take precedence.

1. Count the base image as the first image and distribute all supplied evolution images as evenly as possible across breakthrough stages 1 through 5.
2. The first image starts at stage 1 and the final image starts at stage 5. For intermediate images, use the nearest evenly spaced stage between them.
3. Store only actual visual change points. `BreakthroughVariantStages` contains only the activation stages, and `BreakthroughSprites` contains each distinct image once in the corresponding order. Do not create selectable stages that repeat the same image.
4. Apply the same mapping in both `CardCatalog.asset` and `CreateOrSyncCardCatalog.cs`.

Default examples:

- One image: no visual evolution; keep it as `CardSprite` and leave breakthrough image arrays empty unless another rule requires them.
- Two images: `BreakthroughVariantStages = [1, 5]`, sprites `[1, 2]`.
- Three images: `BreakthroughVariantStages = [1, 3, 5]`, sprites `[1, 2, 3]`.
- Four images: `BreakthroughVariantStages = [1, 2, 4, 5]`, sprites `[1, 2, 3, 4]`.
- Five images: `BreakthroughVariantStages = [1, 2, 3, 4, 5]`, sprites `[1, 2, 3, 4, 5]`.

If more than five distinct images appear to be evolution states, do not silently discard or reinterpret them. Determine whether they are animation/effect frames from names and composition; if that cannot be established safely, report the ambiguity before mapping.
