
# SummerGUI 🚀

## The Modern X-Platform GUI Framework for C# Developers

**SummerGUI** is a high-performance, cross-platform UI framework built for the modern era. Written in **C#**, powered by **OpenTK**, and licensed under the **MIT License**, it empowers you to create stunning desktop applications that run seamlessly on **Windows** and **Ubuntu**.

Are you a C# developer used to the comfort of Windows development but want to break free into the world of high-performance, cross-platform OpenGL? **SummerGUI is for you.** ---

## ✨ What’s New? (The 2026 Evolution)
We have completely rebuilt the engine to meet modern standards. No more legacy code—just pure performance.

- 🎯 **.NET 8 Framework:** Built on the latest LTS release for maximum speed, security, and modern C# language features.

- 💎 **OpenGL Core Profile:** We’ve ditched the old Compatibility Profile. SummerGUI now talks directly to modern GPUs, ensuring compatibility with the latest drivers and hardware.

- ⚡ **VBO Rendering (No more Immediate Mode):** By moving away from glBegin/glEnd to Vertex Buffer Objects (VBOs) and indexed rendering, SummerGUI pushes geometry to the GPU with incredible efficiency.

- 🖋️ **World-Class Typography:** We integrated HarfBuzz alongside FreeType. This means professional-grade text shaping, correct kerning, and beautiful ligatures for any language.

- 🛠️ **Top-Modern Stack:** Every library, from OpenTK 4.x to the latest native bindings, has been updated to the newest versions.

## 🌟 Why SummerGUI?

### Familiar Feel, Modern Power
If you come from a Windows background, you'll feel right at home. We provide a responsive **Layout System, Widget Hierarchy**, and **Event Handling** that feels intuitive, while the underlying OpenGL engine handles the heavy lifting.

### Easy Widget Creation
Creating your own components is a breeze! Use familiar Draw-functions to build custom UI elements without having to become a math genius. Our internal **Batcher** handles the draw calls for you—fast, batched, and optimized.

### Why Core Profile & VBOs matter?
The move to the **Core Profile** and **VBOs** isn't just a technical detail—it's about survival and speed. Modern GPUs are designed to process large buffers of data at once. By batching your UI into VBOs, SummerGUI reduces CPU-to-GPU overhead to a minimum. **The result? Fluid 60+ FPS interfaces, even with complex layouts.** 🚀

## 🛠 Features at a Glance
- **True Cross-Platform:** Write once on Windows/VS-Code, run on Ubuntu/Linux without recompilation.
- **Open & Free:** MIT License means you own your code, even for commercial projects.
- **Fully Scalable:** Built-in support for UI zooming and High-DPI displays.
- **Animations & Themes:** Smooth transitions and a flexible Icon-Font/Theming system.
- **Thread Safe:** Designed to be robust and reliable under heavy load.
- **Blazing Fast:** "Fu*** fast" isn't just a claim; it's our architecture.

## 🔗 Learn More
Check out the **SummerGUI Demo Application** and see it in action:

[👉 SummerGUI.Demo](https://github.com/kroll-software/SummerGUI.Demo)  

## 🤝 Join the Journey
This is an exciting stage of development! SummerGUI is evolving rapidly, and we are looking for early adopters and contributors. If you are familiar with:

- Cross-platform window handling (GLFW/OpenTK)
- Native shell and clipboard integration
- Advanced GLSL Shaders

...we would love to see your Pull Requests!

### 📄 License

Licensed under the **MIT License**. Copyright © 2015-2026 by Kroll Software-Entwicklung.

Note: This software is provided "as is", without warranty of any kind. See the full license text in the repository for details.

Ready to build something beautiful?
Clone the repo, open it in VS-Code, and start your next OpenGL journey today! 🏁
