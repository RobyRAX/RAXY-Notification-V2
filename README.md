# RAXY Notification 2

RAXY Notification 2 shows notifications from a manager prefab. Each definition has its own queue. Non-fullscreen notifications spawn into a container. Fullscreen notifications use a view that already exists in the scene.

## Features

- **NotificationManager** — one prefab, one queue per definition
- **Wait / List** — wait for the previous instance to finish, or show several at once with an interval
- **FullscreenNotificationView** — close button and a single scene instance
- **Project Hub** — generate request and view scripts, assign definitions onto the manager prefab

## Setup

1. Install **RAXY Notification 2** from **Tools → RAXY → Package Installer**.
2. Open **Tools → RAXY → Project Hub** and select **Notification**.
3. Generate a Notification Manager prefab, add entries, then **Generate C#** and **Assign Entries**.
4. Place the manager prefab in a scene and assign each definition's container or fullscreen view.

## Hub Bindings

Each entry can define bindings that become request properties and serialized fields on the generated view:

| Binding | Request type | View field | Applied property |
| --- | --- | --- | --- |
| Text | `string` | `TextMeshProUGUI` | `text` |
| Image | `Sprite` | `Image` | `sprite` |
| Text Color | `Color` | `TextMeshProUGUI` | `color` |
| Image Color | `Color` | `Image` | `color` |
| Image Fill | `float` | `Image` | `fillAmount` |

Set the Image component's **Image Type** to **Filled** to display Image Fill changes. Supply fill values in the `0..1` range. **Read from Project** recognizes generated view fields by their `Tmp`, `Img`, `ColorTmp`, `ColorImg`, and `FillImg` suffixes.

## Samples

A Get Item toast, an Unlock Item fullscreen popup, and a demo scene ship as a Package Manager sample.

1. Open **Window → Package Manager**.
2. Select **RAXY Notification 2**.
3. Under **Samples**, click **Import** on **Notification Demo**.
4. Open the imported **Notification Demo** scene and enter Play Mode.

## Dependencies

- **RAXY Utility** (`com.raxy.utility`) — Project Hub module
- **Unity UI** (`com.unity.ugui`) — buttons, images, and TextMeshPro
