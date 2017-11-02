using System;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace derbaum
{
    public struct Vector3i
    {
        public int X;
        public int Y;
    }

    public class ObjectVertexData
    {
        public int[]     Indices;
        public Vector3[] Vertices;
        public Vector3[] Normals;
        public Vector2[] UVs;
    }

    public enum VertexAttribIndex
    {
        Vertex = 0,
        Normal = 1,
        Uv = 2
    }

    public struct ImageAssetData
    {
        public string AssetName;
        public bool IsLoaded;
        public int OpenGLHandle;
        public bool IsDisplacement;
    }

    public struct MeshAssetData
    {
        public string AssetName;
        public bool IsLoaded;

        public int IndicesCount;
        public int VertexBufferHandle;
        public int IndicesBufferHandle;
        public int VertexArrayObjectHandle;
    }

    public struct BasicShaderAssetData
    {
        public bool IsLoaded;
        public string FragmentShaderName;
        public string VertexShaderName;

        public int VertexObjectHandle;
        public int FragmentObjectHandle;
        public int ProgramHandle;
        public int ModelviewProjectionMatrixLocation;
    }

    public struct LeafShaderAsset
    {
        public BasicShaderAssetData BasicShader;
        public int DisplacementSamplerLocation;
        public int DisplacementScalarLocation;
    }

    public class DerBaumGameWindow : GameWindow
    {
        private bool toggleFullScreen;
        private int updateCounter = 1;
        private double elapsedSeconds = 0;

        private LeafShaderAsset leafShader;

        private ImageAssetData leafColorTexture;
        private ImageAssetData leafDispTexture;
        private MeshAssetData leafMesh;

        public Matrix4 cameraTransformation;
        public Matrix4 cameraPerspectiveProjection;

        public DerBaumGameWindow()
            : base(1280, 720,
                  new GraphicsMode(),
                  "Der Baum",
                  GameWindowFlags.Default,
                  DisplayDevice.Default,
                  3,
                  0,
                  GraphicsContextFlags.ForwardCompatible | GraphicsContextFlags.Debug)
        {
            this.Location = new Point(900, 350);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            EnsureOpenGlVersion();
            EnsureVertexTextureUnits();
            EnsureVertexUniformComponents();

            this.leafColorTexture = new ImageAssetData() {
                AssetName = "textures/leaf_texture.png"
            };
            this.LoadImageAsset(ref this.leafColorTexture);

            this.leafDispTexture = new ImageAssetData() {
                AssetName = "textures/leaf_texture_disp.png"
            };
            this.LoadImageAsset(ref this.leafDispTexture);

            this.leafMesh = new MeshAssetData() {
                AssetName = "meshes/plane_hp.obj"
            };
            this.LoadMeshData(ref this.leafMesh);

            this.leafShader = new LeafShaderAsset {
                BasicShader = new BasicShaderAssetData {
                    VertexShaderName = "shader/Leaf_VS.glsl",
                    FragmentShaderName = "shader/Leaf_FS.glsl"
                },
            };
            this.LoadLeafShaderAsset(ref this.leafShader);

            cameraTransformation = Matrix4.LookAt(new Vector3(0, 0, 3),
                                                  new Vector3(0, 0, 0),
                                                  new Vector3(0, 1, 0));


            GL.Enable(EnableCap.DepthTest);
            GL.ClearColor(1, 1, 1, 1);
        }

        protected override void OnUnload(EventArgs e)
        {
            this.UnloadImageAsset(this.leafColorTexture);
            this.UnloadImageAsset(this.leafDispTexture);
            this.UnloadMeshData(this.leafMesh);
        }

        protected override void OnResize(EventArgs e)
        {
            var fov = 60;
            GL.Viewport(0, 0, Width, Height);
            float aspectRatio = Width / (float)Height;
            Matrix4.CreatePerspectiveFieldOfView((float)(fov * Math.PI / 180.0f),
                                                 aspectRatio,
                                                 1,
                                                 100,
                                                 out this.cameraPerspectiveProjection);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            this.elapsedSeconds += e.Time;
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, leafColorTexture.OpenGLHandle);
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, leafDispTexture.OpenGLHandle);

            GL.BindVertexArray(leafMesh.VertexArrayObjectHandle);
            GL.UseProgram(leafShader.BasicShader.ProgramHandle);

            var transformation = Matrix4.CreateRotationX(updateCounter / 50.0f);
            transformation *= Matrix4.CreateRotationY(updateCounter / 110.0f);
            var modelViewProjection = transformation * 
                                      cameraTransformation * 
                                      cameraPerspectiveProjection;
            GL.UniformMatrix4(
                leafShader.BasicShader.ModelviewProjectionMatrixLocation,
                false,
                ref modelViewProjection);
            GL.Uniform1(leafShader.DisplacementSamplerLocation, 1);
            GL.Uniform1(leafShader.DisplacementScalarLocation, 0.5f);

            GL.DrawElements(PrimitiveType.Triangles,
                            leafMesh.IndicesCount,
                            DrawElementsType.UnsignedInt,
                            IntPtr.Zero);

            CheckOpenGlErrors();

            SwapBuffers();
        }

        private void CheckOpenGlErrors(bool dismiss = false)
        {
            ErrorCode error;
            while((error = GL.GetError()) != ErrorCode.NoError && updateCounter % 30 == 0 && !dismiss) {
                Console.WriteLine($"OpenGL error: {error.ToString()}");
            }
        }

        private void LoadImageAsset(ref ImageAssetData asset)
        {
            if (asset.IsLoaded) return;
            int textureHandle = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, textureHandle);
            Bitmap bmp = new Bitmap(asset.AssetName);
            int width = bmp.Width;
            int height = bmp.Height;
            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                                              ImageLockMode.ReadOnly,
                                              System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            GL.TexImage2D(TextureTarget.Texture2D,
                          0,
                          PixelInternalFormat.Rgba,
                          bmpData.Width,
                          bmpData.Height,
                          0,
                          OpenTK.Graphics.OpenGL.PixelFormat.Bgra,
                          PixelType.UnsignedByte,
                          bmpData.Scan0);
            GL.TexParameter(TextureTarget.Texture2D,
                            TextureParameterName.TextureMinFilter,
                            (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D,
                            TextureParameterName.TextureMagFilter,
                            (int)TextureMinFilter.Nearest);
            bmp.UnlockBits(bmpData);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            asset.OpenGLHandle = textureHandle;
            asset.IsLoaded = true;
        }

        private void UnloadImageAsset(ImageAssetData asset)
        {
            if(asset.IsLoaded) {
                GL.DeleteTexture(asset.OpenGLHandle);
            }
        }

        private void LoadLeafShaderAsset(ref LeafShaderAsset leafShader)
        {
            this.LoadShaderAsset(ref leafShader.BasicShader);
            leafShader.DisplacementSamplerLocation = GL.GetUniformLocation(
                leafShader.BasicShader.ProgramHandle,
                "displacement_sampler");
            leafShader.DisplacementScalarLocation = GL.GetUniformLocation(
                leafShader.BasicShader.ProgramHandle,
                "displacement_scalar");
            Console.WriteLine($"displacement_sampler: {leafShader.DisplacementSamplerLocation}");
            Console.WriteLine($"displacement_scalar: {leafShader.DisplacementScalarLocation}");
        }

        private void UnloadLeafShaderAsset(LeafShaderAsset leafShader)
        {
            this.UnloadShaderAsset(leafShader.BasicShader);
        }

        private void LoadShaderAsset(ref BasicShaderAssetData shaderAsset)
        {
            if (shaderAsset.IsLoaded)
                return;

            string vs = File.ReadAllText(shaderAsset.VertexShaderName);
            string fs = File.ReadAllText(shaderAsset.FragmentShaderName);

            int status_code;
            string info;

            int vertexObject = GL.CreateShader(ShaderType.VertexShader);
            int fragmentObject = GL.CreateShader(ShaderType.FragmentShader);
            shaderAsset.VertexObjectHandle = vertexObject;
            shaderAsset.FragmentObjectHandle = fragmentObject;

            GL.ShaderSource(vertexObject, vs);
            GL.CompileShader(vertexObject);
            GL.GetShaderInfoLog(vertexObject, out info);
            GL.GetShader(vertexObject, ShaderParameter.CompileStatus, out status_code);

            if (status_code != 1)
                throw new ApplicationException(info);

            GL.ShaderSource(fragmentObject, fs);
            GL.CompileShader(fragmentObject);
            GL.GetShaderInfoLog(fragmentObject, out info);
            GL.GetShader(fragmentObject, ShaderParameter.CompileStatus, out status_code);

            if (status_code != 1)
                throw new ApplicationException(info);

            int program = GL.CreateProgram();
            GL.AttachShader(program, fragmentObject);
            GL.AttachShader(program, vertexObject);
            shaderAsset.ProgramHandle = program;

            GL.BindAttribLocation(program, (int)VertexAttribIndex.Vertex, "in_position");
            GL.BindAttribLocation(program, (int)VertexAttribIndex.Normal, "in_normal");
            GL.BindAttribLocation(program, (int)VertexAttribIndex.Uv, "in_uv");

            GL.LinkProgram(program);

            shaderAsset.ModelviewProjectionMatrixLocation = GL.GetUniformLocation(
                program,
                "modelview_projection_matrix");
            Console.WriteLine($"modelview_projection_matrix: {shaderAsset.ModelviewProjectionMatrixLocation}");
        }

        private void UnloadShaderAsset(BasicShaderAssetData shaderAsset)
        {
            if(shaderAsset.IsLoaded) {
                GL.DeleteProgram(shaderAsset.ProgramHandle);
                GL.DeleteShader(shaderAsset.FragmentObjectHandle);
                GL.DeleteShader(shaderAsset.VertexObjectHandle);
            }
        }

        private void LoadMeshData(ref MeshAssetData meshAsset)
        {
            if (meshAsset.IsLoaded)
                return;

            var planeMesh = Wavefront.Load(meshAsset.AssetName);
            PushMeshToGPUBuffer(planeMesh, ref meshAsset);
            meshAsset.IndicesCount = planeMesh.Indices.Length;
            meshAsset.IsLoaded = true;
        }

        private void UnloadMeshData(MeshAssetData meshAsset)
        {
            if(meshAsset.IsLoaded) {
                GL.DeleteVertexArray(meshAsset.VertexArrayObjectHandle);
                GL.DeleteBuffer(meshAsset.VertexBufferHandle);
                GL.DeleteBuffer(meshAsset.IndicesBufferHandle);
            }
        }

        private void PushMeshToGPUBuffer(ObjectVertexData mesh, ref MeshAssetData assetData)
        {
            var interleaved = new float[8 * mesh.Vertices.Length];
            var interleavedIndex = 0;
            for (int i = 0; i < mesh.Vertices.Length; i++) {
                interleavedIndex = i * 8;
                interleaved[interleavedIndex++] = mesh.Vertices[i].X;
                interleaved[interleavedIndex++] = mesh.Vertices[i].Y;
                interleaved[interleavedIndex++] = mesh.Vertices[i].Z;

                interleaved[interleavedIndex++] = mesh.Normals[i].X;
                interleaved[interleavedIndex++] = mesh.Normals[i].Y;
                interleaved[interleavedIndex++] = mesh.Normals[i].Z;

                interleaved[interleavedIndex++] = mesh.UVs[i].X;
                interleaved[interleavedIndex++] = mesh.UVs[i].Y;
            }

            int vertexBufferHandle;
            GL.GenBuffers(1, out vertexBufferHandle);
            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferHandle);
            GL.BufferData(BufferTarget.ArrayBuffer,
                          interleaved.Length  * sizeof(float),
                          interleaved,
                          BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            assetData.VertexBufferHandle = vertexBufferHandle;

            int indexBufferHandle;
            GL.GenBuffers(1, out indexBufferHandle);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBufferHandle);
            GL.BufferData(BufferTarget.ElementArrayBuffer,
                          sizeof(uint) * mesh.Indices.Length,
                          mesh.Indices,
                          BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
            assetData.IndicesBufferHandle = indexBufferHandle;

            int vertexArrayObjectHandle;
            GL.GenVertexArrays(1, out vertexArrayObjectHandle);
            GL.BindVertexArray(vertexArrayObjectHandle);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBufferHandle);
            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferHandle);
            GL.EnableVertexAttribArray(0);
            GL.EnableVertexAttribArray(1);
            GL.EnableVertexAttribArray(2);

            var stride = 2 * (Vector3.SizeInBytes) + Vector2.SizeInBytes;
            GL.VertexAttribPointer((int)VertexAttribIndex.Vertex,
                                   3,
                                   VertexAttribPointerType.Float,
                                   true,
                                   stride,
                                   0);

            GL.VertexAttribPointer((int)VertexAttribIndex.Normal,
                                   3,
                                   VertexAttribPointerType.Float,
                                   true,
                                   stride,
                                   Vector3.SizeInBytes);
            GL.VertexAttribPointer((int)VertexAttribIndex.Uv,
                                   2,
                                   VertexAttribPointerType.Float,
                                   true,
                                   stride,
                                   Vector3.SizeInBytes * 2);
            GL.BindVertexArray(0);
            assetData.VertexArrayObjectHandle = vertexArrayObjectHandle;
        }

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            if (e.Key == Key.Enter && e.Modifiers == KeyModifiers.Alt) {
                this.toggleFullScreen = true;
            }
            else {
                base.OnKeyDown(e);
            }
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            if (Keyboard[Key.Escape]) {
                this.Exit();
            }

            if (this.toggleFullScreen) {
                if (WindowState != WindowState.Fullscreen) {
                    WindowState = WindowState.Fullscreen;
                }
                else {
                    WindowState = WindowState.Normal;
                }
                this.toggleFullScreen = false;
            }
            updateCounter++;
        }

        private static void EnsureVertexUniformComponents()
        {
            var maxVertexTextureUnits = GL.GetInteger(GetPName.MaxVertexUniformComponents);
            if (maxVertexTextureUnits < 20) {
                throw new NotSupportedException("Not enough Vertex Uniform slots");
            }
        }

        private static void EnsureVertexTextureUnits()
        {
            var maxVertexTextureUnits = GL.GetInteger(GetPName.MaxVertexTextureImageUnits);
            if (maxVertexTextureUnits < 5) {
                throw new NotSupportedException("Not enough Vertex Texture slots");
            }
        }

        private static void EnsureOpenGlVersion()
        {
            Version version = new Version(GL.GetString(StringName.Version).Substring(0, 3));
            Version target = new Version(2, 0);
            if (version < target) {
                throw new NotSupportedException(String.Format(
                    "OpenGL {0} is required (you only have {1}).", target, version));
            }
        }

    }
}
