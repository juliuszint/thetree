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
        public Vector3[] Tangents;
        public Vector3[] BiTangents;
    }

    public static class VertexAttribIndex
    {
        public const int Vertex = 0;
        public const int Normal = 1;
        public const int Uv = 2;
        public const int Tangent = 3;
        public const int Bitangent = 4;
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

    public struct BlinnShaderAsset
    {
        public BasicShaderAssetData BasicShader;
    }

    public struct LeafShaderAsset
    {
        public BasicShaderAssetData BasicShader;
        public int DisplacementSamplerLocation;
        public int DisplacementScalarLocation;
    }

    public struct CameraData
    {
        public float XAngle;
        public float YAngle;
        public Vector3 Position;

        public Matrix4 Transformation;
        public Matrix4 PerspectiveProjection;
    }

    public class DerBaumGameWindow : GameWindow
    {
        private bool toggleFullScreen;
        private int updateCounter = 1;
        private double elapsedSeconds = 0;

        private LeafShaderAsset leafShader;
        private BlinnShaderAsset blinnShader;
        private BasicShaderAssetData basicShader;

        private ImageAssetData leafColorTexture;
        private ImageAssetData leafDispTexture;
        private ImageAssetData brownTexture;
        private ImageAssetData emptyNormalTexture;
        private MeshAssetData leafMesh;
        private MeshAssetData treeMesh;
        private CameraData camera;

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
            int tick = Environment.TickCount;
            Console.WriteLine("begin loading assets");
            base.OnLoad(e);
            EnsureOpenGlVersion();
            EnsureVertexTextureUnits();
            EnsureVertexUniformComponents();

            this.emptyNormalTexture = new ImageAssetData() {
                AssetName = "textures/empty_normal.jpg"
            };
            this.LoadImageAsset(ref this.emptyNormalTexture);

            this.brownTexture = new ImageAssetData() {
                AssetName = "textures/brown.jpg"
            };
            this.LoadImageAsset(ref this.brownTexture);

            this.leafColorTexture = new ImageAssetData() {
                AssetName = "textures/leaf_texture.png"
            };
            this.LoadImageAsset(ref this.leafColorTexture);

            this.leafDispTexture = new ImageAssetData() {
                AssetName = "textures/leaf_texture_disp.jpg"
            };
            this.LoadImageAsset(ref this.leafDispTexture);

            this.leafMesh = new MeshAssetData() {
                AssetName = "meshes/leaf.obj"
            };
            this.treeMesh = new MeshAssetData() {
                AssetName = "meshes/tree.obj"
            };
            this.LoadMeshData(ref this.treeMesh);
            this.LoadMeshData(ref this.leafMesh);

            this.basicShader = new BasicShaderAssetData {
                VertexShaderName = "shader/Leaf_VS.glsl",
                FragmentShaderName = "shader/Leaf_FS.glsl"
            };
            this.blinnShader = new BlinnShaderAsset {
                BasicShader = new BasicShaderAssetData {
                    VertexShaderName = "shader/Blinn_VS.glsl",
                    FragmentShaderName = "shader/Blinn_FS.glsl"
                },
            };
            this.leafShader = new LeafShaderAsset {
                BasicShader = new BasicShaderAssetData {
                    VertexShaderName = "shader/Leaf_VS.glsl",
                    FragmentShaderName = "shader/Leaf_FS.glsl"
                },
            };
            this.LoadShaderAsset(ref this.basicShader);
            this.LoadLeafShaderAsset(ref this.leafShader);
            this.LoadBlinnShaderAsset(ref this.blinnShader);

            this.camera.Transformation = Matrix4.LookAt(new Vector3(0, 0, 20),
                                                        new Vector3(0, 0, 0),
                                                        new Vector3(0, 1, 0));
            this.camera.Position = new Vector3(0, 3, 10);


            GL.Enable(EnableCap.DepthTest);
            //GL.ClearColor(.63f, .28f, 0.64f, 1);
            GL.ClearColor(1.0f, 1.0f, 1.0f, 1);
            var time = Environment.TickCount - tick;
            Console.WriteLine($"done loading assets in: {time / 1000.0}s");
        }

        protected override void OnUnload(EventArgs e)
        {
            this.UnloadImageAsset(this.emptyNormalTexture);
            this.UnloadImageAsset(this.brownTexture);
            this.UnloadImageAsset(this.leafColorTexture);
            this.UnloadImageAsset(this.leafDispTexture);
            this.UnloadMeshData(this.leafMesh);
            this.UnloadMeshData(this.treeMesh);
            this.UnloadLeafShaderAsset(this.leafShader);
            this.UnloadShaderAsset(this.basicShader);
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
                                                 out this.camera.PerspectiveProjection);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            this.elapsedSeconds += e.Time;
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            MoveCamera(ref this.camera, (float)e.Time, 6.0f, 1.0f);
            var viewProjection = this.camera.Transformation * this.camera.PerspectiveProjection;

            RenderTree(viewProjection);

            CheckOpenGlErrors();
            SwapBuffers();
        }

        private void MoveCamera(ref CameraData camera,
                                float fTimeDelta,
                                float translationSens,
                                float rotationSens)
        {
            if(Keyboard[Key.A]) {
                var ortho = Vector3.Cross(
                    camera.Transformation.Column1.Xyz,
                    camera.Transformation.Column2.Xyz);
                camera.Position -= ortho * translationSens * fTimeDelta;
            }
            if(Keyboard[Key.D]) {
                var ortho = Vector3.Cross(
                    camera.Transformation.Column1.Xyz,
                    camera.Transformation.Column2.Xyz);
                camera.Position += ortho * translationSens * fTimeDelta;
            }
            if(Keyboard[Key.W]) {
                camera.Position -= new Vector3(
                    camera.Transformation.Column2.X,
                    camera.Transformation.Column2.Y,
                    camera.Transformation.Column2.Z) * translationSens * fTimeDelta;
            }
            if(Keyboard[Key.S]) {
                camera.Position += new Vector3(
                    camera.Transformation.Column2.X,
                    camera.Transformation.Column2.Y,
                    camera.Transformation.Column2.Z) * translationSens * fTimeDelta;
            }
            if(Keyboard[Key.E]) {
                camera.Position += new Vector3(
                    camera.Transformation.Column1.X,
                    camera.Transformation.Column1.Y,
                    camera.Transformation.Column1.Z) * translationSens * fTimeDelta;
            }
            if(Keyboard[Key.Q]) {
                camera.Position -= new Vector3(
                    camera.Transformation.Column1.X,
                    camera.Transformation.Column1.Y,
                    camera.Transformation.Column1.Z) * translationSens * fTimeDelta;
            }
            if(Keyboard[Key.Up]) {
                camera.XAngle -= rotationSens * fTimeDelta;
            }
            if(Keyboard[Key.Down]) {
                camera.XAngle += rotationSens * fTimeDelta;
            }
            if(Keyboard[Key.Left]) {
                camera.YAngle -= rotationSens * fTimeDelta;
            }
            if(Keyboard[Key.Right]) {
                camera.YAngle += rotationSens * fTimeDelta;
            }

            camera.Transformation = Matrix4.Identity;
            camera.Transformation *= Matrix4.CreateTranslation(
                -camera.Position.X,
                -camera.Position.Y,
                -camera.Position.Z);
            camera.Transformation *= Matrix4.CreateRotationX(camera.XAngle);
            camera.Transformation *= Matrix4.CreateRotationY(camera.YAngle);
        }

        private float DegreesToRadians(float degree)
        {
            var result = (float)(degree * (Math.PI / 180));
            return result;
        }

        private void RenderTree(Matrix4 modelViewProjection)
        {
            GL.BindTexture(TextureTarget.Texture2D, brownTexture.OpenGLHandle);
            GL.BindVertexArray(treeMesh.VertexArrayObjectHandle);
            GL.UseProgram(blinnShader.BasicShader.ProgramHandle);

            GL.UniformMatrix4(
                blinnShader.BasicShader.ModelviewProjectionMatrixLocation,
                false,
                ref modelViewProjection);

            GL.DrawElements(PrimitiveType.Triangles,
                            treeMesh.IndicesCount,
                            DrawElementsType.UnsignedInt,
                            IntPtr.Zero);
        }

        private void RenderLeaf(Matrix4 modelViewProjection)
        {
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, leafColorTexture.OpenGLHandle);
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, leafDispTexture.OpenGLHandle);
            GL.BindVertexArray(leafMesh.VertexArrayObjectHandle);
            GL.UseProgram(leafShader.BasicShader.ProgramHandle);

            var transformation = Matrix4.CreateScale(0.2f);
            transformation *= Matrix4.CreateRotationX(90.0f);
            GL.UniformMatrix4(
                leafShader.BasicShader.ModelviewProjectionMatrixLocation,
                false,
                ref modelViewProjection);
            GL.Uniform1(leafShader.DisplacementSamplerLocation, 1);
            GL.Uniform1(leafShader.DisplacementScalarLocation, 0.2f);

            GL.DrawElements(PrimitiveType.Triangles,
                            leafMesh.IndicesCount,
                            DrawElementsType.UnsignedInt,
                            IntPtr.Zero);
        }

        private void RenderLeafSphere()
        {

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

        private void LoadBlinnShaderAsset(ref BlinnShaderAsset shader)
        {
            this.LoadShaderAsset(ref shader.BasicShader);
        }

        private void LoadLeafShaderAsset(ref LeafShaderAsset shader)
        {
            this.LoadShaderAsset(ref shader.BasicShader);
            shader.DisplacementSamplerLocation = GL.GetUniformLocation(
                shader.BasicShader.ProgramHandle,
                "displacement_sampler");
            shader.DisplacementScalarLocation = GL.GetUniformLocation(
                shader.BasicShader.ProgramHandle,
                "displacement_scalar");
            //Console.WriteLine($"displacement_sampler: {shader.DisplacementSamplerLocation}");
            //Console.WriteLine($"displacement_scalar: {shader.DisplacementScalarLocation}");
        }

        private void UnloadLeafShaderAsset(LeafShaderAsset shader)
        {
            this.UnloadShaderAsset(shader.BasicShader);
        }

        private void UnloadBlinnShaderAsset(BlinnShaderAsset shader)
        {
            this.UnloadShaderAsset(shader.BasicShader);
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
            shaderAsset.IsLoaded = true;
        }

        private void UnloadShaderAsset(BasicShaderAssetData shaderAsset)
        {
            if(shaderAsset.IsLoaded) {
                GL.DeleteProgram(shaderAsset.ProgramHandle);
                GL.DeleteShader(shaderAsset.FragmentObjectHandle);
                GL.DeleteShader(shaderAsset.VertexObjectHandle);
            }
            shaderAsset.IsLoaded = false;
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
            int strideCount = 14;
            var interleaved = new float[strideCount * mesh.Vertices.Length];
            var interleavedIndex = 0;
            for (int i = 0; i < mesh.Vertices.Length; i++) {
                interleavedIndex = i * strideCount;
                interleaved[interleavedIndex++] = mesh.Vertices[i].X;
                interleaved[interleavedIndex++] = mesh.Vertices[i].Y;
                interleaved[interleavedIndex++] = mesh.Vertices[i].Z;

                interleaved[interleavedIndex++] = mesh.Normals[i].X;
                interleaved[interleavedIndex++] = mesh.Normals[i].Y;
                interleaved[interleavedIndex++] = mesh.Normals[i].Z;

                interleaved[interleavedIndex++] = mesh.UVs[i].X;
                interleaved[interleavedIndex++] = mesh.UVs[i].Y;

                interleaved[interleavedIndex++] = mesh.Tangents[i].X;
                interleaved[interleavedIndex++] = mesh.Tangents[i].Y;
                interleaved[interleavedIndex++] = mesh.Tangents[i].Z;

                interleaved[interleavedIndex++] = mesh.BiTangents[i].X;
                interleaved[interleavedIndex++] = mesh.BiTangents[i].Y;
                interleaved[interleavedIndex++] = mesh.BiTangents[i].Z;
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
            GL.EnableVertexAttribArray(3);
            GL.EnableVertexAttribArray(4);

            var stride = (4 * Vector3.SizeInBytes) + Vector2.SizeInBytes;
            GL.VertexAttribPointer(VertexAttribIndex.Vertex,
                                   3,
                                   VertexAttribPointerType.Float,
                                   true,
                                   stride,
                                   0);

            GL.VertexAttribPointer(VertexAttribIndex.Normal,
                                   3,
                                   VertexAttribPointerType.Float,
                                   true,
                                   stride,
                                   Vector3.SizeInBytes);
            GL.VertexAttribPointer(VertexAttribIndex.Uv,
                                   2,
                                   VertexAttribPointerType.Float,
                                   true,
                                   stride,
                                   2 * Vector3.SizeInBytes);
            GL.VertexAttribPointer(VertexAttribIndex.Tangent,
                                   3,
                                   VertexAttribPointerType.Float,
                                   true,
                                   stride,
                                   2 * Vector3.SizeInBytes + Vector2.SizeInBytes);
            GL.VertexAttribPointer(VertexAttribIndex.Bitangent,
                                   3,
                                   VertexAttribPointerType.Float,
                                   true,
                                   stride,
                                   3 * Vector3.SizeInBytes + Vector2.SizeInBytes);
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
