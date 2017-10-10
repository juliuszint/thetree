using System;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.IO;
using OpenTK.Input;

namespace derbaum
{
    public class DerBaumGameWindow : GameWindow
    {
        private static float angle = 0.0f;

        private int vertex_shader_object;
        private int fragment_shader_object;
        private int shader_program;

        private int vertex_buffer_object;
        private int color_buffer_object;
        private int element_buffer_object;
        private bool toggleFullScreen;

        public Vector3[] Vertices;
        public int[] Indices;
        public int[] Colors;

        public DerBaumGameWindow()
            : base(800, 600, GraphicsMode.Default)
        { }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // Check for necessary capabilities:
            Version version = new Version(GL.GetString(StringName.Version).Substring(0, 3));
            Version target = new Version(2, 0);
            if (version < target) {
                throw new NotSupportedException(String.Format(
                    "OpenGL {0} is required (you only have {1}).", target, version));
            }

            this.Vertices = new Vector3[] {
                new Vector3(-1.0f, -1.0f,  1.0f),
                new Vector3( 1.0f, -1.0f,  1.0f),
                new Vector3( 1.0f,  1.0f,  1.0f),
                new Vector3(-1.0f,  1.0f,  1.0f),
                new Vector3(-1.0f, -1.0f, -1.0f),
                new Vector3( 1.0f, -1.0f, -1.0f),
                new Vector3( 1.0f,  1.0f, -1.0f),
                new Vector3(-1.0f,  1.0f, -1.0f)
            };

            this.Indices = new int[] {
                0, 1, 2, 2, 3, 0,
                3, 2, 6, 6, 7, 3,
                7, 6, 5, 5, 4, 7,
                4, 0, 3, 3, 7, 4,
                0, 1, 5, 5, 4, 0,
                1, 5, 6, 6, 2, 1,
            };
            this.Colors = new int[]
            {
                0xff0000,
                0xffff00,
                0x00ffff,
                0xff00ff,
                0x0000ff,
                0x00ff00,
                0x00ff66,
                0xff7744
            };

            GL.Enable(EnableCap.DepthTest);
            CreateVBO();

            StreamReader vs = null;
            StreamReader fs = null;
            try {
                vs = new StreamReader("shader/Simple_VS.glsl");
                fs = new StreamReader("shader/Simple_FS.glsl");

                CreateShaders(vs.ReadToEnd(),
                              fs.ReadToEnd(),
                              out vertex_shader_object,
                              out fragment_shader_object,
                              out shader_program);
            }
            finally {
                vs?.Dispose();
                fs?.Dispose();
            }
        }

       

        void CreateShaders(string vs, string fs,
            out int vertexObject, out int fragmentObject,
            out int program)
        {
            int status_code;
            string info;

            vertexObject = GL.CreateShader(ShaderType.VertexShader);
            fragmentObject = GL.CreateShader(ShaderType.FragmentShader);

            // Compile vertex shader
            GL.ShaderSource(vertexObject, vs);
            GL.CompileShader(vertexObject);
            GL.GetShaderInfoLog(vertexObject, out info);
            GL.GetShader(vertexObject, ShaderParameter.CompileStatus, out status_code);

            if (status_code != 1)
                throw new ApplicationException(info);

            // Compile vertex shader
            GL.ShaderSource(fragmentObject, fs);
            GL.CompileShader(fragmentObject);
            GL.GetShaderInfoLog(fragmentObject, out info);
            GL.GetShader(fragmentObject, ShaderParameter.CompileStatus, out status_code);

            if (status_code != 1)
                throw new ApplicationException(info);

            program = GL.CreateProgram();
            GL.AttachShader(program, fragmentObject);
            GL.AttachShader(program, vertexObject);

            GL.LinkProgram(program);
            GL.UseProgram(program);
        }

   

        void CreateVBO()
        {
            int size;

            GL.GenBuffers(1, out vertex_buffer_object);
            GL.GenBuffers(1, out color_buffer_object);
            GL.GenBuffers(1, out element_buffer_object);

            // Upload the vertex buffer.
            GL.BindBuffer(BufferTarget.ArrayBuffer, vertex_buffer_object);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(Vertices.Length * 3 * sizeof(float)), Vertices,
                BufferUsageHint.StaticDraw);

            GL.GetBufferParameter(BufferTarget.ArrayBuffer, BufferParameterName.BufferSize, out size);
            if (size != Vertices.Length * 3 * sizeof(Single))
                throw new ApplicationException(String.Format(
                    "Problem uploading vertex buffer to VBO (vertices). Tried to upload {0} bytes, uploaded {1}.",
                    Vertices.Length * 3 * sizeof(Single), size));
           
            // Upload the color buffer.
            GL.BindBuffer(BufferTarget.ArrayBuffer, color_buffer_object);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(Colors.Length * sizeof(int)), Colors,
                BufferUsageHint.StaticDraw);
            GL.GetBufferParameter(BufferTarget.ArrayBuffer, BufferParameterName.BufferSize, out size);
            if (size != Colors.Length * sizeof(int))
                throw new ApplicationException(String.Format(
                    "Problem uploading vertex buffer to VBO (colors). Tried to upload {0} bytes, uploaded {1}.",
                    Colors.Length * sizeof(int), size));

            // Upload the index buffer (elements inside the vertex buffer, not color indices as per the IndexPointer function!)
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, element_buffer_object);
            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(Indices.Length * sizeof(Int32)), Indices,
                BufferUsageHint.StaticDraw);
            GL.GetBufferParameter(BufferTarget.ElementArrayBuffer, BufferParameterName.BufferSize, out size);
            if (size != Indices.Length * sizeof(int))
                throw new ApplicationException(String.Format(
                    "Problem uploading vertex buffer to VBO (offsets). Tried to upload {0} bytes, uploaded {1}.",
                    Indices.Length * sizeof(int), size));
        }

      

        protected override void OnUnload(EventArgs e)
        {
            if (shader_program != 0)
                GL.DeleteProgram(shader_program);
            if (fragment_shader_object != 0)
                GL.DeleteShader(fragment_shader_object);
            if (vertex_shader_object != 0)
                GL.DeleteShader(vertex_shader_object);
            if (vertex_buffer_object != 0)
                GL.DeleteBuffers(1, ref vertex_buffer_object);
            if (element_buffer_object != 0)
                GL.DeleteBuffers(1, ref element_buffer_object);
        }

       
       
        protected override void OnResize(EventArgs e)
        {
            GL.Viewport(0, 0, Width, Height);

            float aspect_ratio = Width / (float)Height;
            Matrix4 perpective = Matrix4.CreatePerspectiveFieldOfView(MathHelper.PiOver4, aspect_ratio, 1, 64);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix(ref perpective);
        }

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            if(e.Key == Key.Enter && e.Modifiers == KeyModifiers.Alt) {
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

            if(this.toggleFullScreen) {
                if (WindowState != WindowState.Fullscreen) {
                    WindowState = WindowState.Fullscreen;
                }
                else {
                    WindowState = WindowState.Normal;
                }
                this.toggleFullScreen = false;
            }
        }


        protected override void OnRenderFrame(FrameEventArgs e)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit |
                     ClearBufferMask.DepthBufferBit);

            Matrix4 lookat = Matrix4.LookAt(0, 5, 5, 0, 0, 0, 0, 1, 0);

            GL.MatrixMode(MatrixMode.Modelview);

            GL.LoadMatrix(ref lookat);

            angle += (float)e.Time;
            GL.Rotate(angle * 4, 1.0f, 1.0f, 0.0f);
            GL.Rotate(angle * 5, 0.0f, 1.0f, 0.0f);
            GL.Rotate(angle * 5, 0.0f, 0.0f, 1.0f);

            GL.EnableClientState(ArrayCap.VertexArray);
            GL.EnableClientState(ArrayCap.ColorArray);

            GL.BindBuffer(BufferTarget.ArrayBuffer, vertex_buffer_object);
            GL.VertexPointer(3, VertexPointerType.Float, 0, IntPtr.Zero);
            GL.BindBuffer(BufferTarget.ArrayBuffer, color_buffer_object);
            GL.ColorPointer(4, ColorPointerType.UnsignedByte, 0, IntPtr.Zero);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, element_buffer_object);

            GL.DrawElements(BeginMode.Triangles, Indices.Length,
                DrawElementsType.UnsignedInt, IntPtr.Zero);

            //GL.DrawArrays(GL.Enums.BeginMode.POINTS, 0, shape.Vertices.Length);

            GL.DisableClientState(ArrayCap.VertexArray);
            GL.DisableClientState(ArrayCap.ColorArray);


            //int error = GL.GetError();
            //if (error != 0)
            //    Debug.Print(Glu.ErrorString(Glu.Enums.ErrorCode.INVALID_OPERATION));

            SwapBuffers();
        }

        
    }
}
