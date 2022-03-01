using System;
using OpenTK.Graphics.OpenGL;
using System.IO;
using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace derbaum
{
    public struct Vector3i
    {
        public int X;
        public int Y;
    }

    public class BoundingBox
    {
        public float MinX;
        public float MaxX;
        public float MinZ;
        public float MaxZ;
        public float MinY;
        public float MaxY;
    }

    public enum LeafColor
    {
        Green,
        Yellow,
        Red,
        Brown,
    }

    public class LeafSimulationData
    {
        public Vector3 RotationAxis;
        public float RotationAngle;

        public Vector3 PositionOrigin;
        public Vector3 Position;
        public Vector3 Velocity;
        public float FallDelay;
        public float LeafScaleOrigin;
        public float LeafScale;

        public float ColorEntropy;
        public LeafColor PrimaryLeafColor;
        public LeafColor SecondaryLeafColor;
        public float LeafColorFraction;
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
        public int ModelMatrixLocation;
        public int ColorTextureLocation;
        public int NormalTextureLocation;
        public int MaterialShininessLocation;
        public int LightDirectionLocation;
        public int LightAmbientColorLocation;
        public int LightDiffuseColorLocation;
        public int LightSpecularColorLocation;
        public int CameraPositionLocation;
    }

    public struct LeafShaderAsset
    {
        public BasicShaderAssetData BasicShader;
        public int ModelMatrixLocation;
        public int ColorTextureLocationOne;
        public int ColorTextureLocationTwo;
        public int NormalTextureLocation;
        public int MaterialShininessLocation;
        public int LightDirectionLocation;
        public int LightAmbientColorLocation;
        public int LightDiffuseColorLocation;
        public int LightSpecularColorLocation;
        public int TextureFraction;
        public int CameraPositionLocation;
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
        private bool cameraOverride;
        private int updateCounter = 1;
        private double elapsedSeconds = 0;

        private Vector3 ambientLightDirection;
        private LeafShaderAsset leafShader;
        private BlinnShaderAsset blinnShader;
        private BasicShaderAssetData basicShader;

        private ImageAssetData leafColorTexture;
        private ImageAssetData leafColorTextureGreen;
        private ImageAssetData leafColorTextureYellow;
        private ImageAssetData leafColorTextureRed;
        private ImageAssetData leafColorTextureBrown;
        private ImageAssetData potColorTexture;
        private ImageAssetData brownTexture;
        private ImageAssetData emptyNormalTexture;
        private ImageAssetData rockNormalTexture;
        private ImageAssetData wallNormalTexture;
        private MeshAssetData leafMesh;
        private MeshAssetData treeMesh;
        private MeshAssetData cubeMesh;
        private MeshAssetData planeMesh;
        private MeshAssetData potMesh;
        private MeshAssetData icoSphereMesh;
        private CameraData camera;

        private float simLoopDetection;
        private LeafSimulationData[] leafSimData;
        private BoundingBox potBoundingBox;


        public DerBaumGameWindow(GameWindowSettings s, NativeWindowSettings ns)
            : base(s, ns)
            
        {
        }


        protected override void OnLoad()
        {
            int tick = Environment.TickCount;
            Console.WriteLine("begin loading assets");
            base.OnLoad();
            EnsureOpenGlVersion();
            EnsureVertexTextureUnits();
            EnsureVertexUniformComponents();

            this.emptyNormalTexture = new ImageAssetData() {
                AssetName = "textures/empty_normal.jpg"
            };
            this.rockNormalTexture = new ImageAssetData() {
                AssetName = "textures/rock_normal.jpg"
            };
            this.wallNormalTexture = new ImageAssetData() {
                AssetName = "textures/wall_normal.png"
            };
            this.brownTexture = new ImageAssetData() {
                AssetName = "textures/brown.jpg"
            };
            this.leafColorTexture = new ImageAssetData() {
                AssetName = "textures/leaf_texture.png"
            };
            this.potColorTexture = new ImageAssetData() {
                AssetName = "textures/pot_texture.png"
            };
            this.leafColorTextureGreen = new ImageAssetData() {
                AssetName = "textures/leaf_texture_green.png"
            };
            this.leafColorTextureYellow = new ImageAssetData() {
                AssetName = "textures/leaf_texture_yellow.png"
            };
            this.leafColorTextureRed = new ImageAssetData() {
                AssetName = "textures/leaf_texture_red.png"
            };
            this.leafColorTextureBrown = new ImageAssetData() {
                AssetName = "textures/leaf_texture_brown.png"
            };
            this.LoadImageAsset(ref this.wallNormalTexture);
            this.LoadImageAsset(ref this.potColorTexture);
            this.LoadImageAsset(ref this.emptyNormalTexture);
            this.LoadImageAsset(ref this.rockNormalTexture);
            this.LoadImageAsset(ref this.leafColorTexture);
            this.LoadImageAsset(ref this.leafColorTextureGreen);
            this.LoadImageAsset(ref this.leafColorTextureYellow);
            this.LoadImageAsset(ref this.leafColorTextureRed);
            this.LoadImageAsset(ref this.leafColorTextureBrown);
            this.LoadImageAsset(ref this.brownTexture);

            this.leafMesh = new MeshAssetData() {
                AssetName = "meshes/leaf.obj"
            };
            this.planeMesh = new MeshAssetData() {
                AssetName = "meshes/plane.obj"
            };
            this.treeMesh = new MeshAssetData() {
                AssetName = "meshes/tree.obj"
            };
            this.cubeMesh = new MeshAssetData() {
                AssetName = "meshes/cube.obj"
            };
            this.icoSphereMesh = new MeshAssetData() {
                AssetName = "meshes/icosphere.obj"
            };
            this.potMesh = new MeshAssetData() {
                AssetName = "meshes/pot.obj"
            };
            this.LoadMeshData(ref this.treeMesh);
            this.LoadMeshData(ref this.planeMesh);
            this.LoadMeshData(ref this.cubeMesh);
            this.LoadMeshData(ref this.icoSphereMesh);
            this.LoadMeshData(ref this.leafMesh);
            this.potBoundingBox = this.LoadMeshData(ref this.potMesh, true);

            this.InitLeafSimulationData();

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
            this.ambientLightDirection = new Vector3(0, -1, 0);
            this.ambientLightDirection.Normalize();

            GL.Enable(EnableCap.DepthTest);
            //GL.ClearColor(.63f, .28f, 0.64f, 1);
            GL.ClearColor(1.0f, 1.0f, 1.0f, 1);
            var time = Environment.TickCount - tick;
            Console.WriteLine($"done loading assets in: {time / 1000.0}s");
        }

        protected override void OnUnload()
        {
            this.UnloadImageAsset(this.emptyNormalTexture);
            this.UnloadImageAsset(this.brownTexture);
            this.UnloadImageAsset(this.leafColorTexture);
            this.UnloadImageAsset(this.rockNormalTexture);
            this.UnloadImageAsset(this.leafColorTextureGreen);
            this.UnloadImageAsset(this.leafColorTextureYellow);
            this.UnloadImageAsset(this.leafColorTextureRed);
            this.UnloadImageAsset(this.leafColorTextureBrown);

            this.UnloadMeshData(this.leafMesh);
            this.UnloadMeshData(this.planeMesh);
            this.UnloadMeshData(this.treeMesh);
            this.UnloadMeshData(this.potMesh);
            this.UnloadMeshData(this.cubeMesh);
            this.UnloadMeshData(this.icoSphereMesh);

            this.UnloadLeafShaderAsset(this.leafShader);
            this.UnloadShaderAsset(this.basicShader);
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            var fov = 60;
            GL.Viewport(0, 0, e.Width, e.Height);
            float aspectRatio = e.Width / (float)e.Height;
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
            if(cameraOverride) {
                MoveCamera(ref this.camera, (float)e.Time, 6.0f, 1.0f);
            }
            else {
                RotateCamera(ref this.camera);
            }
            var viewProjection = this.camera.Transformation * this.camera.PerspectiveProjection;

            //var scale = (float)(((Math.Sin(elapsedSeconds) + 1) / 2) * 1.2);
            var scale = 1;
            Vector3 treePosition = new Vector3(0, 1.65f, 0);

            SimulateLeafs((float)e.Time, (float)this.elapsedSeconds);

            RenderTree(treePosition);
            RenderLeafs(treePosition, scale);
            RenderPot();

            CheckOpenGlErrors();
            SwapBuffers();
        }

        private void RenderPot()
        {
            this.RenderWithBlinn(
                ref this.potMesh,
                ref this.potColorTexture,
                ref this.emptyNormalTexture,
                new Vector3(0,0,0));
        }

        private void RenderTree(Vector3 pos)
        {
            this.RenderWithBlinn(
                ref this.treeMesh,
                ref this.brownTexture,
                ref this.emptyNormalTexture,
                pos);
        }

        private void RenderWithBlinn(
            ref MeshAssetData mesh,
            ref ImageAssetData colorTexture,
            ref ImageAssetData normalTexture,
            Vector3 location)
        {
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, colorTexture.OpenGLHandle);
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, normalTexture.OpenGLHandle);

            GL.BindVertexArray(mesh.VertexArrayObjectHandle);
            GL.UseProgram(blinnShader.BasicShader.ProgramHandle);

            Matrix4 modelMatrix = Matrix4.Identity * Matrix4.CreateTranslation(location);
            var modelViewProjection = modelMatrix * this.camera.Transformation * this.camera.PerspectiveProjection;
            GL.UniformMatrix4(
                blinnShader.BasicShader.ModelviewProjectionMatrixLocation,
                false,
                ref modelViewProjection);

            GL.UniformMatrix4(blinnShader.ModelMatrixLocation, false, ref modelMatrix);
            GL.Uniform1(blinnShader.ColorTextureLocation, 0);
            GL.Uniform1(blinnShader.NormalTextureLocation, 1);
            GL.Uniform1(blinnShader.MaterialShininessLocation, 2.0f);
            GL.Uniform3(blinnShader.LightDirectionLocation, this.ambientLightDirection);

            GL.Uniform4(blinnShader.LightAmbientColorLocation, new Vector4(0.6f, 0.6f, 0.6f, 0));
            GL.Uniform4(blinnShader.LightDiffuseColorLocation, new Vector4(0.8f, 0.8f, 0.8f, 0));
            GL.Uniform4(blinnShader.LightSpecularColorLocation, new Vector4(0.0f, 0.0f, 0.0f, 0));
            GL.Uniform4(blinnShader.CameraPositionLocation, new Vector4(this.camera.Position, 1));

            GL.DrawElements(PrimitiveType.Triangles,
                            mesh.IndicesCount,
                            DrawElementsType.UnsignedInt,
                            IntPtr.Zero);
            GL.BindVertexArray(0);
            GL.ActiveTexture(TextureUnit.Texture0);

        }

        private void RenderLeafs(Vector3 offset, float scale)
        {
            GL.BindVertexArray(planeMesh.VertexArrayObjectHandle);
            GL.UseProgram(leafShader.BasicShader.ProgramHandle);
            GL.ActiveTexture(TextureUnit.Texture2);
            GL.BindTexture(TextureTarget.Texture2D, emptyNormalTexture.OpenGLHandle);
            GL.Uniform1(leafShader.ColorTextureLocationOne, 0);
            GL.Uniform1(leafShader.ColorTextureLocationTwo, 1);
            GL.Uniform1(leafShader.NormalTextureLocation, 2);

            GL.Uniform4(leafShader.LightAmbientColorLocation, new Vector4(0.8f, 0.8f, 0.8f, 0));
            GL.Uniform4(leafShader.LightDiffuseColorLocation, new Vector4(0.9f, 0.9f, 0.9f, 0));
            GL.Uniform4(leafShader.LightSpecularColorLocation, new Vector4(0.0f, 0.0f, 0.0f, 0.0f));
            GL.Uniform4(leafShader.CameraPositionLocation, new Vector4(this.camera.Position, 1));
            GL.Uniform3(leafShader.LightDirectionLocation, ambientLightDirection);

            GL.Uniform1(leafShader.MaterialShininessLocation, 0.0f);

            for(int i = 0; i < this.leafSimData.Length; i++) {
                var simData = this.leafSimData[i];
                var modelMatrix = Matrix4.CreateFromAxisAngle(simData.RotationAxis, simData.RotationAngle) * 
                                  Matrix4.CreateScale(simData.LeafScale) *
                                  Matrix4.CreateTranslation(simData.Position + offset);

                var modelViewProjection = modelMatrix * 
                                          this.camera.Transformation * 
                                          this.camera.PerspectiveProjection;
                GL.UniformMatrix4(
                    leafShader.BasicShader.ModelviewProjectionMatrixLocation,
                    false,
                    ref modelViewProjection);

                var primaryTextureHandle = this.ToTextureHandle(simData.PrimaryLeafColor);
                var secondaryTextureHandle = this.ToTextureHandle(simData.SecondaryLeafColor);
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, primaryTextureHandle);
                GL.ActiveTexture(TextureUnit.Texture1);
                GL.BindTexture(TextureTarget.Texture2D, secondaryTextureHandle);
                GL.UniformMatrix4(leafShader.ModelMatrixLocation, false, ref modelMatrix);
                GL.Uniform1(leafShader.TextureFraction, simData.LeafColorFraction);

                GL.DrawElements(PrimitiveType.Triangles,
                                planeMesh.IndicesCount,
                                DrawElementsType.UnsignedInt,
                                IntPtr.Zero);
            }
        }

        private void RotateCamera(ref CameraData camera)
        {
            var transformation = Matrix4.CreateRotationY((float)this.elapsedSeconds / 3);
            transformation *= Matrix4.CreateTranslation(new Vector3(0, -3, -10));
            camera.Transformation = transformation;
        }

        private void MoveCamera(ref CameraData camera,
                                float fTimeDelta,
                                float translationSens,
                                float rotationSens)
        {
            if(KeyboardState[Keys.A]) {
                var ortho = Vector3.Cross(
                    camera.Transformation.Column1.Xyz,
                    camera.Transformation.Column2.Xyz);
                camera.Position -= ortho * translationSens * fTimeDelta;
            }
            if(KeyboardState[Keys.D]) {
                var ortho = Vector3.Cross(
                    camera.Transformation.Column1.Xyz,
                    camera.Transformation.Column2.Xyz);
                camera.Position += ortho * translationSens * fTimeDelta;
            }
            if(KeyboardState[Keys.W]) {
                camera.Position -= new Vector3(
                    camera.Transformation.Column2.X,
                    camera.Transformation.Column2.Y,
                    camera.Transformation.Column2.Z) * translationSens * fTimeDelta;
            }
            if(KeyboardState[Keys.S]) {
                camera.Position += new Vector3(
                    camera.Transformation.Column2.X,
                    camera.Transformation.Column2.Y,
                    camera.Transformation.Column2.Z) * translationSens * fTimeDelta;
            }
            if(KeyboardState[Keys.E]) {
                camera.Position += new Vector3(
                    camera.Transformation.Column1.X,
                    camera.Transformation.Column1.Y,
                    camera.Transformation.Column1.Z) * translationSens * fTimeDelta;
            }
            if(KeyboardState[Keys.Q]) {
                camera.Position -= new Vector3(
                    camera.Transformation.Column1.X,
                    camera.Transformation.Column1.Y,
                    camera.Transformation.Column1.Z) * translationSens * fTimeDelta;
            }
            if(KeyboardState[Keys.Up]) {
                //camera.XAngle -= rotationSens * fTimeDelta;
            }
            if(KeyboardState[Keys.Down]) {
                //camera.XAngle += rotationSens * fTimeDelta;
            }
            if(KeyboardState[Keys.Left]) {
                camera.YAngle -= rotationSens * fTimeDelta;
            }
            if(KeyboardState[Keys.Right]) {
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

        private void SimulateLeafs(float timeDelta, float fTime)
        {
            if (this.elapsedSeconds < 1) {
                return;
            }

            Vector2 leafGrowTime      = new Vector2(0 , 2);
            Vector2 summerTime        = new Vector2(2 , 5);
            Vector2 leafFallTime      = new Vector2(5 , 15);
            Vector2 leafDisappearTime = new Vector2(15, 17);
            Vector2 winterTime        = new Vector2(17, 18);
            float intervalTime = winterTime.Y;
            
            Vector2 greenYellowColorInterval = new Vector2(3, 6);
            Vector2 yellowRedColorInterval   = new Vector2(6, 10);
            Vector2 redBrownColorInterval    = new Vector2(10, 13);

            bool updateScale = false;
            bool updatePosition = false;
            var scaleFactor = 0.0f;
            var simTime = (((float)this.elapsedSeconds - 1) % intervalTime);
            if(this.simLoopDetection > simTime) {
                this.ResetSimulationData();
            }
            this.simLoopDetection = simTime;

            var relGrowTime = IsInTimeInterval(leafGrowTime, simTime);
            if(relGrowTime >= 0) {
                scaleFactor = relGrowTime * relGrowTime;
                updateScale = true;
            }
            var relLeafFallTime = IsInTimeInterval(leafFallTime, simTime);
            if (relLeafFallTime >= 0) {
                updatePosition = true;
            }
            var relLeafDisappearTime = IsInTimeInterval(leafDisappearTime, simTime);
            if(relLeafDisappearTime >= 0) {
                var inverse = 1 - relLeafDisappearTime;
                scaleFactor = inverse * inverse;
                updateScale = true;
            }

            for(int i = 0; i < this.leafSimData.Length; i++) {
                var entry = this.leafSimData[i];
                if(updateScale) {
                    this.leafSimData[i].LeafScale = entry.LeafScaleOrigin * scaleFactor; 
                }
                if(updatePosition) {
                    this.MoveLeaf(i, timeDelta, fTime);
                }

                var colorTime = simTime + ((entry.ColorEntropy * 6.0f) - 2.0f);
                colorTime = Math.Min(colorTime, intervalTime);
                colorTime = Math.Max(colorTime, 0);
                var greenYellowFraction = IsInTimeInterval(greenYellowColorInterval, colorTime);
                if(greenYellowFraction >= 0) {
                    this.leafSimData[i].LeafColorFraction = greenYellowFraction;
                    this.leafSimData[i].PrimaryLeafColor = LeafColor.Green; 
                    this.leafSimData[i].SecondaryLeafColor = LeafColor.Yellow; 
                }
                var yellowRedFraction = IsInTimeInterval(yellowRedColorInterval, colorTime);
                if(yellowRedFraction >= 0) {
                    this.leafSimData[i].LeafColorFraction = yellowRedFraction;
                    this.leafSimData[i].PrimaryLeafColor = LeafColor.Yellow; 
                    this.leafSimData[i].SecondaryLeafColor = LeafColor.Red; 
                }
                var redBrownFraction = IsInTimeInterval(redBrownColorInterval, colorTime);
                if(redBrownFraction >= 0) {
                    this.leafSimData[i].LeafColorFraction = redBrownFraction;
                    this.leafSimData[i].PrimaryLeafColor = LeafColor.Red; 
                    this.leafSimData[i].SecondaryLeafColor = LeafColor.Brown; 
                }
            }
        }

        private void MoveLeaf(int index, float timeDelta, float fTime)
        {
            var simData = this.leafSimData[index];
            var distance_delta = timeDelta * this.leafSimData[index].Velocity;
            var collisionOffsetForVertices = -potBoundingBox.MaxY;
            if(IsOverFlowerPot(this.leafSimData[index].Position)) {
                collisionOffsetForVertices = 0;
            }

            if(simData.Position.Y <= collisionOffsetForVertices) {
                return;
            }
            else if((simData.FallDelay * 10 - fTime) < 0) {
                this.leafSimData[index].Position -= distance_delta;
            }

        }

        private float IsInTimeInterval(Vector2 interval, float time)
        {
            var result = -1.0f;
            if(interval.X <= time && interval.Y >= time) {
                result = (time - interval.X) / (interval.Y - interval.X);
            }
            return result;
        }

        private bool IsOverFlowerPot(Vector3 v)
        {
            var result = false;
            if(v.X > potBoundingBox.MinX &&
               v.X < potBoundingBox.MaxX &&
               v.Z > potBoundingBox.MinZ &&
               v.Z < potBoundingBox.MaxZ) {
                result = true;
            }
            return result;
        }

        private float RadiansToDegrees(float radians)
        {
            var result = (float)((radians * 180) / Math.PI);
            return result;
        }
        private float DegreesToRadians(float degree)
        {
            var result = (float)(degree * (Math.PI / 180));
            return result;
        }

        private void CheckOpenGlErrors(bool dismiss = false)
        {
            OpenTK.Graphics.OpenGL.ErrorCode error;
            while((error = GL.GetError()) != OpenTK.Graphics.OpenGL.ErrorCode.NoError && updateCounter % 30 == 0 && !dismiss) {
                Console.WriteLine($"OpenGL error: {error.ToString()}");
            }
        }

        private void LoadImageAsset(ref ImageAssetData asset)
        {
            if (asset.IsLoaded) return;
            int textureHandle = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, textureHandle);
            var image = SixLabors.ImageSharp.Image.Load(asset.AssetName);
            var imageData = image.CloneAs<SixLabors.ImageSharp.PixelFormats.Bgra32>();

            byte[] pixelBytes = new byte[image.Width * image.Height * 4];
            imageData.CopyPixelDataTo(pixelBytes);
            GL.TexImage2D(TextureTarget.Texture2D,
                          0,
                          PixelInternalFormat.Rgba,
                          imageData.Width,
                          imageData.Height,
                          0,
                          PixelFormat.Bgra,
                          PixelType.UnsignedByte,
                          pixelBytes);
            GL.TexParameter(TextureTarget.Texture2D,
                            TextureParameterName.TextureMinFilter,
                            (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D,
                            TextureParameterName.TextureMagFilter,
                            (int)TextureMinFilter.Nearest);
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
            GL.BindAttribLocation(shader.BasicShader.ProgramHandle, VertexAttribIndex.Tangent, "in_tangent");
            GL.BindAttribLocation(shader.BasicShader.ProgramHandle, VertexAttribIndex.Bitangent, "in_bitangent");

            shader.ModelMatrixLocation = GL.GetUniformLocation(shader.BasicShader.ProgramHandle, "model_matrix");
            shader.MaterialShininessLocation = GL.GetUniformLocation(shader.BasicShader.ProgramHandle, "specular_shininess");
            shader.LightDirectionLocation = GL.GetUniformLocation(shader.BasicShader.ProgramHandle, "light_direction");
            shader.LightAmbientColorLocation = GL.GetUniformLocation(shader.BasicShader.ProgramHandle, "light_ambient_color");
            shader.LightDiffuseColorLocation = GL.GetUniformLocation(shader.BasicShader.ProgramHandle, "light_diffuse_color");
            shader.LightSpecularColorLocation = GL.GetUniformLocation(shader.BasicShader.ProgramHandle, "light_specular_color");
            shader.CameraPositionLocation = GL.GetUniformLocation(shader.BasicShader.ProgramHandle, "camera_position");
            shader.ColorTextureLocation = GL.GetUniformLocation(shader.BasicShader.ProgramHandle, "color_texture");
            shader.NormalTextureLocation = GL.GetUniformLocation(shader.BasicShader.ProgramHandle, "normalmap_texture");
        }

        private void LoadLeafShaderAsset(ref LeafShaderAsset shader)
        {
            this.LoadShaderAsset(ref shader.BasicShader);
            GL.BindAttribLocation(shader.BasicShader.ProgramHandle, VertexAttribIndex.Tangent, "in_tangent");
            GL.BindAttribLocation(shader.BasicShader.ProgramHandle, VertexAttribIndex.Bitangent, "in_bitangent");

            shader.ModelMatrixLocation = GL.GetUniformLocation(shader.BasicShader.ProgramHandle, "model_matrix");
            shader.MaterialShininessLocation = GL.GetUniformLocation(shader.BasicShader.ProgramHandle, "specular_shininess");
            shader.TextureFraction = GL.GetUniformLocation(shader.BasicShader.ProgramHandle, "texture_fraction");
            shader.LightDirectionLocation = GL.GetUniformLocation(shader.BasicShader.ProgramHandle, "light_direction");
            shader.LightAmbientColorLocation = GL.GetUniformLocation(shader.BasicShader.ProgramHandle, "light_ambient_color");
            shader.LightDiffuseColorLocation = GL.GetUniformLocation(shader.BasicShader.ProgramHandle, "light_diffuse_color");
            shader.LightSpecularColorLocation = GL.GetUniformLocation(shader.BasicShader.ProgramHandle, "light_specular_color");
            shader.CameraPositionLocation = GL.GetUniformLocation(shader.BasicShader.ProgramHandle, "camera_position");
            shader.ColorTextureLocationOne = GL.GetUniformLocation(shader.BasicShader.ProgramHandle, "color_texture_one");
            shader.ColorTextureLocationTwo = GL.GetUniformLocation(shader.BasicShader.ProgramHandle, "color_texture_two");
            shader.NormalTextureLocation = GL.GetUniformLocation(shader.BasicShader.ProgramHandle, "normalmap_texture");
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

            GL.BindAttribLocation(program, VertexAttribIndex.Vertex, "in_position");
            GL.BindAttribLocation(program, VertexAttribIndex.Normal, "in_normal");
            GL.BindAttribLocation(program, VertexAttribIndex.Uv, "in_uv");

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

        private BoundingBox LoadMeshData(ref MeshAssetData meshAsset, bool calcBoundingBox = false)
        {
            var result = new BoundingBox();
            if (meshAsset.IsLoaded)
                return result;

            var planeMesh = Wavefront.Load(meshAsset.AssetName);
            if(calcBoundingBox) {
                for(int i = 0; i < planeMesh.Vertices.Length; i++) {
                    result.MaxX = Math.Max(planeMesh.Vertices[i].X, result.MaxX);
                    result.MinX = Math.Min(planeMesh.Vertices[i].X, result.MinX);
                    result.MaxZ = Math.Max(planeMesh.Vertices[i].Z, result.MaxZ);
                    result.MinZ = Math.Min(planeMesh.Vertices[i].Z, result.MinZ);
                    result.MaxY = Math.Max(planeMesh.Vertices[i].Y, result.MaxY);
                    result.MinY = Math.Min(planeMesh.Vertices[i].Y, result.MinY);
                }
            }

            PushMeshToGPUBuffer(planeMesh, ref meshAsset);
            meshAsset.IndicesCount = planeMesh.Indices.Length;
            meshAsset.IsLoaded = true;
            return result;
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
            if (e.Key == Keys.Enter && e.Modifiers == KeyModifiers.Alt) {
                this.toggleFullScreen = true;
            }
            if(e.Key == Keys.Space) {
                this.cameraOverride = true;
            }
            else {
                base.OnKeyDown(e);
            }
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            if (KeyboardState[Keys.Escape]) {
                this.Close();
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

        private void InitLeafSimulationData()
        {
            var random = new Random(43523);
            var leafPositionsSource = Wavefront.LoadPlain("meshes/leaf_positions.obj");
            this.leafSimData = new LeafSimulationData[leafPositionsSource.Vertices.Length];
            for(int i = 0; i < leafPositionsSource.Vertices.Length; i++) {
                var distorationFactor = .7f;
                var vertex = leafPositionsSource.Vertices[i];
                var normal = leafPositionsSource.Normals[i] * -1;
                var leafDirection = new Vector3(0, 0, -1);
                normal.Normalize();
                var angle = Vector3.CalculateAngle(leafDirection, normal);
                var angleDegrees = RadiansToDegrees(angle);
                var rotationAxis = Vector3.Cross(leafDirection, normal);
                this.leafSimData[i] = new LeafSimulationData();
                this.leafSimData[i].RotationAngle = angle;
                this.leafSimData[i].RotationAxis = rotationAxis;
                this.leafSimData[i].PositionOrigin = leafPositionsSource.Vertices[i];
                this.leafSimData[i].Position = leafPositionsSource.Vertices[i];
                this.leafSimData[i].FallDelay = (float)random.NextDouble();
                this.leafSimData[i].LeafScaleOrigin = Math.Min((float)random.NextDouble() * 2.5f, 1.2f);
                this.leafSimData[i].LeafScale = 0;
                this.leafSimData[i].ColorEntropy = (float)random.NextDouble();
                this.leafSimData[i].Velocity = new Vector3(
                    (float)(random.NextDouble() - 0.5f) * distorationFactor,
                    1,
                    (float)(random.NextDouble() - 0.5f) * distorationFactor);
            }
        }

        private void ResetSimulationData()
        {
            for (int i = 0; i < this.leafSimData.Length; i++) {
                this.leafSimData[i].Position = this.leafSimData[i].PositionOrigin;
                this.leafSimData[i].LeafColorFraction = 0;
                this.leafSimData[i].PrimaryLeafColor = LeafColor.Green; 
                this.leafSimData[i].SecondaryLeafColor = LeafColor.Green; 
            }
        }

        private int ToTextureHandle(LeafColor color)
        {
            var result = this.leafColorTextureGreen.OpenGLHandle;
            switch(color) {
                case LeafColor.Green:
                    result = this.leafColorTextureGreen.OpenGLHandle;
                    break;
                case LeafColor.Yellow:
                    result = this.leafColorTextureYellow.OpenGLHandle;
                    break;
                case LeafColor.Red:
                    result = this.leafColorTextureRed.OpenGLHandle;
                    break;
                case LeafColor.Brown:
                    result = this.leafColorTextureBrown.OpenGLHandle;
                    break;
            }
            return result;
        }
    }
}
