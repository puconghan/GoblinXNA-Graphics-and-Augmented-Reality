using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

using GoblinXNA;
using GoblinXNA.Graphics;
using GoblinXNA.SceneGraph;
using Model = GoblinXNA.Graphics.Model;
using GoblinXNA.Graphics.Geometry;
using GoblinXNA.Device.Capture;
using GoblinXNA.Device.Vision;
using GoblinXNA.Device.Vision.Marker;
using GoblinXNA.Device.Util;
using GoblinXNA.Device.Generic;
using GoblinXNA.Physics;
using GoblinXNA.Physics.Newton1;
using GoblinXNA.Helpers;
using GoblinXNA.Shaders;
using GoblinXNA.UI.UI2D;
using GoblinXNA.UI;

namespace TrackingCamera
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class TrackingCam : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;

        Scene scene;
        MarkerNode groundMarkerNode, toolbarMarkerNode;
        GeometryNode humvee, gears, cup, g36c, pointingMarker;
        Boolean humveeflag = false;
        Boolean gearsflag = false;
        Boolean cupflag = false;
        Boolean g36cflag = false;
        Boolean collusionflag = false;
        SpriteFont textFont, textFont1;
        float humveeSize = 0;
        float gearsSize = 0;
        float cupSize = 0;
        float g36cSize = 0;
        Matrix humveeTrans = new Matrix();
        Matrix gearsTrans = new Matrix();
        Matrix cupTrans = new Matrix();
        Matrix g36cTrans = new Matrix();
        float humveeX = 0;
        float humveeY = 0;
        float humveeZ = 0;
        float gearsX = 0;
        float gearsY = 0;
        float gearsZ = 0;
        float cupX = 0;
        float cupY = 0;
        float cupZ = 0;
        float g36cX = 0;
        float g36cY = 0;
        float g36cZ = 0;
        Matrix humveeRotation = new Matrix();
        Matrix gearsRotation = new Matrix();
        Matrix cupRotation = new Matrix();
        Matrix g36cRotation = new Matrix();
        Matrix humveeMatrix = new Matrix();
        Matrix gearsMatrix = new Matrix();
        Matrix cupMatrix = new Matrix();
        Matrix g36cMatrix = new Matrix();
        G2DPanel objectFrame;
        ButtonGroup group1, group2;
        G2DSlider slider;
        String rotationAxis = "";
        String rotationDirection = "";
        int rotationSpeed = 0;
        Boolean rotationFlag = false;
        Boolean translationModeFlag = false;
        Boolean transferFlag = false;
        Boolean rotationObjectFlag = false;
        Boolean panelTrigger = false;
        Boolean scaleFlag = false;
        Boolean resetFlag = false;

        float markerSize = 32.4f;
        String label = "Nothing is selected";

        public TrackingCam()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            graphics.PreferredBackBufferWidth = 800;
            graphics.PreferredBackBufferHeight = 600;
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            // Display the mouse cursor
            this.IsMouseVisible = true;

            // Initialize the GoblinXNA framework
            State.InitGoblin(graphics, Content, "");

            // Initialize the scene graph
            scene = new Scene();

            // Use the newton physics engine to perform collision detection
            scene.PhysicsEngine = new NewtonPhysics();

            // For some reason, it sometimes causes memory conflict when it attempts to update the
            // marker transformation in the multi-threaded code, so if you see weird exceptions 
            // thrown in Shaders, then you should not enable the marker tracking thread
            State.ThreadOption = (ushort)ThreadOptions.MarkerTracking;

            // Set up optical marker tracking
            // Note that we don't create our own camera when we use optical marker
            // tracking. It'll be created automatically
            SetupMarkerTracking();

            // Set up the lights used in the scene
            CreateLights();

            // Create 3D objects
            CreateObjects();

            CreateControlPanel();

            // Add a key click callback function.
            KeyboardInput.Instance.KeyPressEvent += new HandleKeyPress(KeyPressHandler);

            objectFrame.Visible = false;
            humveeMatrix = Matrix.CreateFromAxisAngle(new Vector3(1, 0, 0), MathHelper.ToRadians(0));
            gearsMatrix = Matrix.CreateFromAxisAngle(new Vector3(1, 0, 0), MathHelper.ToRadians(0));
            cupMatrix = Matrix.CreateFromAxisAngle(new Vector3(1, 0, 0), MathHelper.ToRadians(0));
            g36cMatrix = Matrix.CreateFromAxisAngle(new Vector3(1, 0, 0), MathHelper.ToRadians(0));
        }

        private void CreateLights()
        {
            // Create a directional light source
            LightSource lightSource = new LightSource();
            lightSource.Direction = new Vector3(1, -1, -1);
            lightSource.Diffuse = Color.White.ToVector4();
            lightSource.Specular = new Vector4(0.6f, 0.6f, 0.6f, 1);

            // Create a light node to hold the light source
            LightNode lightNode = new LightNode();
            lightNode.LightSource = lightSource;

            // Set this light node to cast shadows (by just setting this to true will not cast any shadows,
            // scene.ShadowMap needs to be set to a valid IShadowMap and Model.Shader needs to be set to
            // a proper IShadowShader implementation
            lightNode.CastShadows = true;

            // You should also set the light projection when casting shadow from this light
            lightNode.LightProjection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, 1, 1f, 500);

            scene.RootNode.AddChild(lightNode);
        }

        private void SetupMarkerTracking()
        {
            DirectShowCapture captureDevice = new DirectShowCapture();
            captureDevice.InitVideoCapture(0, FrameRate._60Hz, Resolution._640x480, ImageFormat.R8G8B8_24, false);

            scene.AddVideoCaptureDevice(captureDevice);

            // Use ALVAR marker tracker
            ALVARMarkerTracker tracker = new ALVARMarkerTracker();
            tracker.MaxMarkerError = 0.02f;
            tracker.InitTracker(captureDevice.Width, captureDevice.Height, "calib.xml", markerSize);

            // Set the marker tracker to use for our scene
            scene.MarkerTracker = tracker;

            // Display the camera image in the background.
            scene.ShowCameraImage = true;

            // Create a marker node to track a ground marker array.
            groundMarkerNode = new MarkerNode(scene.MarkerTracker, "ALVARGroundArray.xml");
            // Create a marker node to track a toolbar marker array.
            toolbarMarkerNode = new MarkerNode(scene.MarkerTracker, "ALVARToolbar.xml");
            scene.RootNode.AddChild(groundMarkerNode);
            scene.RootNode.AddChild(toolbarMarkerNode);
        }

        private void CreateObjects()
        {
            // Create humvee model.
            ModelLoader loader = new ModelLoader();
            humvee = new GeometryNode("Humvee");
            humvee.Model = (Model)loader.Load("", "humvee");
            // Add this humvee model to the physics engine for collision detection
            humvee.AddToPhysicsEngine = true;
            humvee.Physics.Shape = ShapeType.ConvexHull;
            // Set model materials.
            ((Model)humvee.Model).UseInternalMaterials = true;
            // Get the dimension of the model.
            Vector3 dimension1 = Vector3Helper.GetDimensions(humvee.Model.MinimumBoundingBox);
            // Scale the model to fit to the size of 5 markers.
            float scale1 = markerSize * (float) 1.5 / Math.Max(dimension1.X, dimension1.Z);
            // Transformation node of the humvee model.
            humveeSize = scale1;
            humveeTrans = Matrix.CreateTranslation(0, 30, 0);
            humveeRotation = Matrix.CreateFromAxisAngle(new Vector3(1, 0, 0), MathHelper.ToRadians(0));

            // Create gears model.
            gears = new GeometryNode("Gears");
            gears.Model = (Model)loader.Load("", "gears");
            // Add this gears model to the physics engine for collision detection
            gears.AddToPhysicsEngine = true;
            gears.Physics.Shape = ShapeType.ConvexHull;
            // Set model materials.
            ((Model)gears.Model).UseInternalMaterials = true;
            // Get the dimension of the model.
            Vector3 dimension2 = Vector3Helper.GetDimensions(gears.Model.MinimumBoundingBox);
            // Scale the model to fit to the size of 5 markers.
            float scale2 = markerSize / Math.Max(dimension2.X, dimension2.Z);
            // Transformation node of the gears model.
            gearsSize = scale2;
            gearsTrans = Matrix.CreateTranslation(0, 110, 0);
            gearsRotation = Matrix.CreateFromAxisAngle(new Vector3(1, 0, 0), MathHelper.ToRadians(60));

            // Create cup model.
            cup = new GeometryNode("Cup");
            cup.Model = (Model)loader.Load("", "bardak");
            // Add this cup model to the physics engine for collision detection
            cup.AddToPhysicsEngine = true;
            cup.Physics.Shape = ShapeType.ConvexHull;
            // Set model materials.
            ((Model)cup.Model).UseInternalMaterials = true;
            // Get the dimension of the model.
            Vector3 dimension3 = Vector3Helper.GetDimensions(cup.Model.MinimumBoundingBox);
            // Scale the model to fit to the size of 5 markers.
            float scale3 = markerSize / Math.Max(dimension3.X, dimension3.Z);
            cupSize = scale3;
            cupTrans = Matrix.CreateTranslation(60, 60, 0);
            cupRotation = Matrix.CreateFromAxisAngle(new Vector3(1, 0, 0), MathHelper.ToRadians(0));

            // Create g36c model.
            g36c = new GeometryNode("G36C Gun");
            g36c.Model = (Model)loader.Load("", "g36c");
            // Add this g36c model to the physics engine for collision detection
            g36c.AddToPhysicsEngine = true;
            g36c.Physics.Shape = ShapeType.ConvexHull;
            // Set model materials.
            ((Model)g36c.Model).UseInternalMaterials = true;
            // Get the dimension of the model.
            Vector3 dimension4 = Vector3Helper.GetDimensions(g36c.Model.MinimumBoundingBox);
            // Scale the model to fit to the size of 5 markers.
            float scale4 = markerSize * 2 / Math.Max(dimension4.X, dimension4.Z);
            g36cSize = scale4;
            g36cTrans = Matrix.CreateTranslation(-60, 80, 0);
            g36cRotation = Matrix.CreateFromAxisAngle(new Vector3(1, 0, 0), MathHelper.ToRadians(0));

            // Toolbar marker
            pointingMarker = new GeometryNode("Marker");
            pointingMarker.Model = new Cylinder(markerSize, 4, markerSize * 3, 40);
            // Add this pointingMarker model to the physics engine for collision detection.
            pointingMarker.AddToPhysicsEngine = true;
            pointingMarker.Physics.Shape = ShapeType.Cylinder;
            // Set model materials.
            Material markerMat = new Material();
            markerMat.Diffuse = Color.BlueViolet.ToVector4();
            markerMat.Specular = Color.White.ToVector4();
            markerMat.SpecularPower = 20;
            pointingMarker.Material = markerMat;
            pointingMarker.Material.Diffuse = new Vector4(pointingMarker.Material.Diffuse.X, pointingMarker.Material.Diffuse.Y, pointingMarker.Material.Diffuse.Z, 0);

            groundMarkerNode.AddChild(humvee);
            groundMarkerNode.AddChild(gears);
            groundMarkerNode.AddChild(cup);
            groundMarkerNode.AddChild(g36c);
            groundMarkerNode.AddChild(pointingMarker);
        }

        private void humveeCollision(NewtonPhysics.CollisionPair pair)
        {
            label = "Humvee";
            Console.WriteLine(label);
            ((Model)gears.Model).ShowBoundingBox = false;
            ((Model)cup.Model).ShowBoundingBox = false;
            ((Model)g36c.Model).ShowBoundingBox = false;
            ((Model)humvee.Model).ShowBoundingBox = true;
        }

        private void gearsCollision(NewtonPhysics.CollisionPair pair)
        {
            label = "Gears";
            Console.WriteLine(label);
            ((Model)cup.Model).ShowBoundingBox = false;
            ((Model)g36c.Model).ShowBoundingBox = false;
            ((Model)humvee.Model).ShowBoundingBox = false;
            ((Model)gears.Model).ShowBoundingBox = true;
        }

        private void cupCollision(NewtonPhysics.CollisionPair pair)
        {
            label = "Cup";
            Console.WriteLine(label);
            ((Model)g36c.Model).ShowBoundingBox = false;
            ((Model)humvee.Model).ShowBoundingBox = false;
            ((Model)gears.Model).ShowBoundingBox = false;
            ((Model)cup.Model).ShowBoundingBox = true;
        }

        private void g36cCollision(NewtonPhysics.CollisionPair pair)
        {
            label = "G36C Gun";
            Console.WriteLine(label);
            ((Model)humvee.Model).ShowBoundingBox = false;
            ((Model)gears.Model).ShowBoundingBox = false;
            ((Model)cup.Model).ShowBoundingBox = false;
            ((Model)g36c.Model).ShowBoundingBox = true;
        }

        void KeyPressHandler(Keys key, KeyModifier modifier)
        {
            if (key == Keys.Escape)
                this.Exit();
            if (key == Keys.R)
            {
                resetFlag = true;
                ((Model)humvee.Model).ShowBoundingBox = false;
                ((Model)gears.Model).ShowBoundingBox = false;
                ((Model)cup.Model).ShowBoundingBox = false;
                ((Model)g36c.Model).ShowBoundingBox = false;
                label = "Nothing is selected";
                objectFrame.Visible = false;
                rotationFlag = false;
                slider.Value = 0;
                rotationSpeed = 0;

                humveeX = 0;
                humveeY = 0;
                humveeZ = 0;
                gearsX = 0;
                gearsY = 0;
                gearsZ = 0;
                cupX = 0;
                cupY = 0;
                cupZ = 0;
                g36cX = 0;
                g36cY = 0;
                g36cZ = 0;

                // Get the dimension of the model.
                Vector3 dimension1 = Vector3Helper.GetDimensions(humvee.Model.MinimumBoundingBox);
                // Scale the model to fit to the size of 5 markers.
                float scale1 = markerSize * (float)1.5 / Math.Max(dimension1.X, dimension1.Z);
                // Transformation node of the humvee model.
                humveeSize = scale1;
                humveeTrans = Matrix.CreateTranslation(0, 30, 0);
                humveeRotation = Matrix.CreateFromAxisAngle(new Vector3(1, 0, 0), MathHelper.ToRadians(0));
                humveeMatrix = Matrix.Identity;


                // Get the dimension of the model.
                Vector3 dimension2 = Vector3Helper.GetDimensions(gears.Model.MinimumBoundingBox);
                // Scale the model to fit to the size of 5 markers.
                float scale2 = markerSize / Math.Max(dimension2.X, dimension2.Z);
                // Transformation node of the gears model.
                gearsSize = scale2;
                gearsTrans = Matrix.CreateTranslation(0, 110, 0);
                gearsRotation = Matrix.CreateFromAxisAngle(new Vector3(1, 0, 0), MathHelper.ToRadians(60));
                gearsMatrix = Matrix.Identity;

                // Get the dimension of the model.
                Vector3 dimension3 = Vector3Helper.GetDimensions(cup.Model.MinimumBoundingBox);
                // Scale the model to fit to the size of 5 markers.
                float scale3 = markerSize / Math.Max(dimension3.X, dimension3.Z);
                cupSize = scale3;
                cupTrans = Matrix.CreateTranslation(60, 60, 0);
                cupRotation = Matrix.CreateFromAxisAngle(new Vector3(1, 0, 0), MathHelper.ToRadians(0));
                cupMatrix = Matrix.Identity;

                // Get the dimension of the model.
                Vector3 dimension4 = Vector3Helper.GetDimensions(g36c.Model.MinimumBoundingBox);
                // Scale the model to fit to the size of 5 markers.
                float scale4 = markerSize * 2 / Math.Max(dimension4.X, dimension4.Z);
                g36cSize = scale4;
                g36cTrans = Matrix.CreateTranslation(-60, 80, 0);
                g36cRotation = Matrix.CreateFromAxisAngle(new Vector3(1, 0, 0), MathHelper.ToRadians(0));
                g36cMatrix = Matrix.Identity;

                if (translationModeFlag == true)
                    translationModeFlag = false;
                if (transferFlag == true)
                    transferFlag = false;
                if (rotationObjectFlag == true)
                    rotationObjectFlag = false;
                if (panelTrigger == true)
                    panelTrigger = false;
                if (scaleFlag == true)
                    scaleFlag = false;

                if (humveeflag == true)
                {
                    toolbarMarkerNode.RemoveChild(humvee);
                    groundMarkerNode.AddChild(humvee);
                    humveeflag = false;
                }
                if (gearsflag == true)
                {
                    toolbarMarkerNode.RemoveChild(gears);
                    groundMarkerNode.AddChild(gears);
                    gearsflag = false;
                }
                if (cupflag == true)
                {
                    toolbarMarkerNode.RemoveChild(cup);
                    groundMarkerNode.AddChild(cup);
                    cupflag = false;
                }
                if (g36cflag == true)
                {
                    toolbarMarkerNode.RemoveChild(g36c);
                    groundMarkerNode.AddChild(g36c);
                    g36cflag = false;
                }
            }
            if (key == Keys.Q)
            {
                ((Model)humvee.Model).ShowBoundingBox = false;
                ((Model)gears.Model).ShowBoundingBox = false;
                ((Model)cup.Model).ShowBoundingBox = false;
                ((Model)g36c.Model).ShowBoundingBox = false;
                label = "Nothing is selected";
                objectFrame.Visible = false;
                rotationFlag = false;
                slider.Value = 0;
                rotationSpeed = 0;
                if (translationModeFlag == true)
                    translationModeFlag = false;
                if (transferFlag == true)
                    transferFlag = false;
                if (rotationObjectFlag == true)
                    rotationObjectFlag = false;
                if (panelTrigger == true)
                    panelTrigger = false;
                if (scaleFlag == true)
                    scaleFlag = false;
                resetFlag = false;
                UI2DRenderer.WriteText(Vector2.Zero, "", Color.Red, textFont1, GoblinEnums.HorizontalAlignment.Right, GoblinEnums.VerticalAlignment.Top);
            }
            if (key == Keys.Up)
            {
                if(label == "Humvee")
                    humveeSize += (float) 0.02;
                if (label == "Gears")
                    gearsSize += (float)0.04;
                if (label == "Cup")
                    cupSize += (float)0.02;
                if (label == "G36C Gun")
                    g36cSize += (float)0.02;
            }
            if (key == Keys.Down)
            {
                if (label == "Humvee")
                    humveeSize -= (float)0.02;
                if (label == "Gears")
                    gearsSize -= (float)0.04;
                if (label == "Cup")
                    cupSize -= (float)0.02;
                if (label == "G36C Gun")
                    g36cSize -= (float)0.02;
            }
            if (key == Keys.T)
            {
                if (label != "Nothing is selected")
                {
                    resetFlag = false;
                    translationModeFlag = true;
                    objectFrame.Visible = false;
                    if (transferFlag == true)
                        transferFlag = false;
                    if (rotationObjectFlag == true)
                        rotationObjectFlag = false;
                    if (panelTrigger == true)
                        panelTrigger = false;
                    if (scaleFlag == true)
                        scaleFlag = false;
                }
                else
                    UI2DRenderer.WriteText(Vector2.Zero, "Please select an object", Color.Red, textFont1, GoblinEnums.HorizontalAlignment.Right, GoblinEnums.VerticalAlignment.Top);
            }
            if (key == Keys.M)
            {
                if (label != "Nothing is selected")
                {
                    resetFlag = false;
                    transferFlag = true;
                    objectFrame.Visible = false;
                    if (translationModeFlag == true)
                        translationModeFlag = false;
                    if (rotationObjectFlag == true)
                        rotationObjectFlag = false;
                    if (panelTrigger == true)
                        panelTrigger = false;
                    if (scaleFlag == true)
                        scaleFlag = false;
                }
                else
                    UI2DRenderer.WriteText(Vector2.Zero, "Please select an object", Color.Red, textFont1, GoblinEnums.HorizontalAlignment.Right, GoblinEnums.VerticalAlignment.Top);
            }
            if (key == Keys.C)
            {
                if (label != "Nothing is selected")
                {
                    resetFlag = false;
                    rotationObjectFlag = true;
                    objectFrame.Visible = false;
                    if (translationModeFlag == true)
                        translationModeFlag = false;
                    if (transferFlag == true)
                        transferFlag = false;
                    if (panelTrigger == true)
                        panelTrigger = false;
                    if (scaleFlag == true)
                        scaleFlag = false;
                }
                else
                    UI2DRenderer.WriteText(Vector2.Zero, "Please select an object", Color.Red, textFont1, GoblinEnums.HorizontalAlignment.Right, GoblinEnums.VerticalAlignment.Top);
            }
            if (key == Keys.S)
            {
                if (label != "Nothing is selected")
                {
                    resetFlag = false;
                    scaleFlag = true;
                    objectFrame.Visible = false;
                    if (translationModeFlag == true)
                        translationModeFlag = false;
                    if (transferFlag == true)
                        transferFlag = false;
                    if (rotationObjectFlag == true)
                        rotationObjectFlag = false;
                    if (panelTrigger == true)
                        panelTrigger = false;
                }
                else
                    UI2DRenderer.WriteText(Vector2.Zero, "Please select an object", Color.Red, textFont1, GoblinEnums.HorizontalAlignment.Right, GoblinEnums.VerticalAlignment.Top);
            }
            if (key == Keys.E)
            {
                if (label != "Nothing is selected")
                {
                    resetFlag = false;
                    panelTrigger = true;
                    if (translationModeFlag == true)
                        translationModeFlag = false;
                    if (transferFlag == true)
                        transferFlag = false;
                    if (rotationObjectFlag == true)
                        rotationObjectFlag = false;
                    if (scaleFlag == true)
                        scaleFlag = false;
                }
                else
                    UI2DRenderer.WriteText(Vector2.Zero, "Please select an object", Color.Red, textFont1, GoblinEnums.HorizontalAlignment.Right, GoblinEnums.VerticalAlignment.Top);
            }
        }

        private void CreateControlPanel()
        {
            objectFrame = new G2DPanel();
            objectFrame.Bounds = new Rectangle(670, State.Height - 200, 120, 190);
            objectFrame.Border = GoblinEnums.BorderFactory.LineBorder;
            objectFrame.BorderColor = Color.Gold;
            // Ranges from 0 (fully transparent) to 1 (fully opaque)
            objectFrame.Transparency = 0.5f;
            //Label for rotation
            G2DLabel label1 = new G2DLabel("Object Rotates along:");
            label1.TextFont = textFont;
            label1.Bounds = new Rectangle(5, 5, 80, 20);
            // Create radio button for x-axis rotation.
            G2DRadioButton radioX = new G2DRadioButton("x-axis");
            radioX.TextFont = textFont;
            radioX.Bounds = new Rectangle(5, 15, 80, 20);
            radioX.ActionPerformedEvent += new ActionPerformed(HandleActionPerformedAxisX);
            // Create radio button for y-axis rotation.
            G2DRadioButton radioY = new G2DRadioButton("y-axis");
            radioY.TextFont = textFont;
            radioY.Bounds = new Rectangle(5, 30, 80, 20);
            radioY.ActionPerformedEvent += new ActionPerformed(HandleActionPerformedAxisY);
            // Create radio button for z-axis rotation.
            G2DRadioButton radioZ = new G2DRadioButton("z-axis");
            radioZ.TextFont = textFont;
            radioZ.Bounds = new Rectangle(5, 45, 80, 20);
            radioZ.ActionPerformedEvent += new ActionPerformed(HandleActionPerformedAxisZ);
            //Create a ButtonGroup object which controls the radio button selections
            group1 = new ButtonGroup();
            group1.Add(radioX);
            group1.Add(radioY);
            group1.Add(radioZ);
            //Label for rotation direction.
            G2DLabel label2 = new G2DLabel("Rotation Direction:");
            label2.TextFont = textFont;
            label2.Bounds = new Rectangle(5, 65, 80, 20);
            // Create radio button for clockwise
            G2DRadioButton clockwise = new G2DRadioButton("Clockwise");
            clockwise.TextFont = textFont;
            clockwise.Bounds = new Rectangle(5, 80, 80, 20);
            clockwise.ActionPerformedEvent += new ActionPerformed(HandleActionPerformedDirection);
            // Create radio button for counterclockwise.
            G2DRadioButton counterclockwise = new G2DRadioButton("Counterclockwise");
            counterclockwise.TextFont = textFont;
            counterclockwise.Bounds = new Rectangle(5, 95, 80, 20);
            counterclockwise.ActionPerformedEvent += new ActionPerformed(HandleActionPerformedDirection);
            //Create a ButtonGroup object which controls the radio button selections.
            group2 = new ButtonGroup();
            group2.Add(clockwise);
            group2.Add(counterclockwise);
            //Label for speed
            G2DLabel label3 = new G2DLabel("Rotation Speed:");
            label3.TextFont = textFont;
            label3.Bounds = new Rectangle(5, 115, 80, 20);
            //Slider for speed of rotation
            slider = new G2DSlider();
            slider.TextFont = textFont;
            slider.Bounds = new Rectangle(5, 145, 110, 20);
            slider.Maximum = 8;
            slider.Minimum = 0;
            slider.MajorTickSpacing = 0;
            slider.MinorTickSpacing = 1;
            slider.Value = 0;
            slider.PaintTicks = true;
            slider.PaintLabels = true;
            slider.StateChangedEvent += new StateChanged(HandleStateChangedSpeed);
            G2DButton reset = new G2DButton("Reset");
            reset.Bounds = new Rectangle(5, 165, 55, 20);
            reset.TextFont = textFont;
            reset.ActionPerformedEvent += new ActionPerformed(HandleActionReset);
            G2DButton rotate = new G2DButton("Rotate");
            rotate.Bounds = new Rectangle(65, 165, 50, 20);
            rotate.ActionPerformedEvent += new ActionPerformed(HandleActionRotate);
            rotate.TextFont = textFont;
            objectFrame.AddChild(label1);
            objectFrame.AddChild(radioX);
            objectFrame.AddChild(radioY);
            objectFrame.AddChild(radioZ);
            objectFrame.AddChild(label2);
            objectFrame.AddChild(clockwise);
            objectFrame.AddChild(counterclockwise);
            objectFrame.AddChild(label3);
            objectFrame.AddChild(slider);
            objectFrame.AddChild(reset);
            objectFrame.AddChild(rotate);
            scene.UIRenderer.Add2DComponent(objectFrame);
        }

        //State change handler function for rotation axis.
        private void HandleActionPerformedAxisX(object source)
        {
            //slider.Value = 0;
            G2DComponent comp = (G2DComponent)source;
            rotationAxis = ((G2DRadioButton)comp).Text;
            if (label == "Humvee")
            {
                if (rotationDirection == "Clockwise")
                {
                    if (humveeX == 0)
                        slider.Value = 0;
                    else
                        slider.Value = (int)(humveeX * (float)100) + 1;
                }
                if (rotationDirection == "Counterclockwise")
                {
                    if (humveeX == 0)
                        slider.Value = 0;
                    else
                        slider.Value = (int)(humveeX * (float)-100) + 1;
                }
            }
            if (label == "Gears")
            {
                if (rotationDirection == "Clockwise")
                {
                    if(gearsX == 0)
                        slider.Value = 0;
                    else
                        slider.Value = (int)(gearsX * (float)100) + 1;
                }
                if (rotationDirection == "Counterclockwise")
                {
                    if (gearsX == 0)
                        slider.Value = 0;
                    else
                        slider.Value = (int)(gearsX * (float)-100) + 1;
                }
            }
            if (label == "Cup")
            {
                if (rotationDirection == "Clockwise")
                {
                    if (cupX == 0)
                        slider.Value = 0;
                    else
                        slider.Value = (int)(cupX * (float)100) + 1;
                }
                if (rotationDirection == "Counterclockwise")
                {
                    if (cupX == 0)
                        slider.Value = 0;
                    else
                        slider.Value = (int)(cupX * (float)-100) + 1;
                }
            }
            if (label == "G36C Gun")
            {
                if (rotationDirection == "Clockwise")
                {
                    if (g36cX == 0)
                        slider.Value = 0;
                    else
                        slider.Value = (int)(g36cX * (float)100) + 1;
                }
                if (rotationDirection == "Counterclockwise")
                {
                    if (g36cX == 0)
                        slider.Value = 0;
                    else
                        slider.Value = (int)(g36cX * (float)-100) + 1;
                }
            }
        }
        //State change handler function for rotation axis.
        private void HandleActionPerformedAxisY(object source)
        {
            //slider.Value = 0;
            G2DComponent comp = (G2DComponent)source;
            rotationAxis = ((G2DRadioButton)comp).Text;
            if (label == "Humvee")
            {
                if (rotationDirection == "Clockwise")
                {
                    if (humveeY == 0)
                        slider.Value = 0;
                    else
                        slider.Value = (int)(humveeY * (float)100) + 1;
                }
                if (rotationDirection == "Counterclockwise")
                {
                    if (humveeY == 0)
                        slider.Value = 0;
                    else
                        slider.Value = (int)(humveeY * (float)-100) + 1;
                }
            }
            if (label == "Gears")
            {
                if (rotationDirection == "Clockwise")
                {
                    if (gearsY == 0)
                        slider.Value = 0;
                    else
                        slider.Value = (int)(gearsY * (float)100) + 1;
                }
                if (rotationDirection == "Counterclockwise")
                {
                    if (gearsY == 0)
                        slider.Value = 0;
                    else
                        slider.Value = (int)(gearsY * (float)-100) + 1;
                }
            }
            if (label == "Cup")
            {
                if (rotationDirection == "Clockwise")
                {
                    if (cupY == 0)
                        slider.Value = 0;
                    else
                        slider.Value = (int)(cupY * (float)100) + 1;
                }
                if (rotationDirection == "Counterclockwise")
                {
                    if (cupY == 0)
                        slider.Value = 0;
                    else
                        slider.Value = (int)(cupY * (float)-100) + 1;
                }
            }
            if (label == "G36C Gun")
            {
                if (rotationDirection == "Clockwise")
                {
                    if (g36cY == 0)
                        slider.Value = 0;
                    else
                        slider.Value = (int)(g36cY * (float)100) + 1;
                }
                if (rotationDirection == "Counterclockwise")
                {
                    if (g36cY == 0)
                        slider.Value = 0;
                    else
                        slider.Value = (int)(g36cY * (float)-100) + 1;
                }
            }
        }
        //State change handler function for rotation axis.
        private void HandleActionPerformedAxisZ(object source)
        {
            //slider.Value = 0;
            G2DComponent comp = (G2DComponent)source;
            rotationAxis = ((G2DRadioButton)comp).Text;
            if (label == "Humvee")
            {
                if (rotationDirection == "Clockwise")
                {
                    if (humveeZ == 0)
                        slider.Value = 0;
                    else
                        slider.Value = (int)(humveeZ * (float)100) + 1;
                }
                if (rotationDirection == "Counterclockwise")
                {
                    if (humveeZ == 0)
                        slider.Value = 0;
                    else
                        slider.Value = (int)(humveeZ * (float)-100) + 1;
                }
            }
            if (label == "Gears")
            {
                if (rotationDirection == "Clockwise")
                {
                    if (gearsZ == 0)
                        slider.Value = 0;
                    else
                        slider.Value = (int)(gearsZ * (float)100) + 1;
                }
                if (rotationDirection == "Counterclockwise")
                {
                    if (gearsZ == 0)
                        slider.Value = 0;
                    else
                        slider.Value = (int)(gearsZ * (float)-100) + 1;
                }
            }
            if (label == "Cup")
            {
                if (rotationDirection == "Clockwise")
                {
                    if (cupZ == 0)
                        slider.Value = 0;
                    else
                        slider.Value = (int)(cupZ * (float)100) + 1;
                }
                if (rotationDirection == "Counterclockwise")
                {
                    if (cupZ == 0)
                        slider.Value = 0;
                    else
                        slider.Value = (int)(cupZ * (float)-100) + 1;
                }
            }
            if (label == "G36C Gun")
            {
                if (rotationDirection == "Clockwise")
                {
                    if (g36cZ == 0)
                        slider.Value = 0;
                    else
                        slider.Value = (int)(g36cZ * (float)100) + 1;
                }
                if (rotationDirection == "Counterclockwise")
                {
                    if (g36cZ == 0)
                        slider.Value = 0;
                    else
                        slider.Value = (int)(g36cZ * (float)-100) + 1;
                }
            }
        }

        //State change handler function for rotation direction.
        private void HandleActionPerformedDirection(object source)
        {
            G2DComponent comp = (G2DComponent)source;
            rotationDirection = ((G2DRadioButton)comp).Text;
        }

        //State change handler function for speed control.
        private void HandleStateChangedSpeed(object source)
        {
            G2DComponent comp = (G2DComponent)source;
            rotationSpeed = ((G2DSlider)comp).Value;
        }

        private void HandleActionReset(object source)
        {
            slider.Value = 0;
            rotationSpeed = 0;
            rotationFlag = false;

            humveeX = 0;
            humveeY = 0;
            humveeZ = 0;
        }

        private void HandleActionRotate(object source)
        {
            rotationFlag = true;
        }

        protected override void LoadContent()
        {
            textFont = Content.Load<SpriteFont>("Arial");
            textFont1 = Content.Load<SpriteFont>("ArialLarge");
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        protected override void Dispose(bool disposing)
        {
            scene.Dispose();
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();
            scene.Update(gameTime.ElapsedGameTime, gameTime.IsRunningSlowly, this.IsActive);
        }

        private void markerHelper(GameTime gameTime)
        {
            if (rotationFlag)
            {
                if (rotationDirection == "Clockwise")
                {
                    if (label == "Humvee")
                    {
                        if (rotationAxis == "x-axis")
                            humveeX = (float) rotationSpeed * (float)0.01;
                        if (rotationAxis == "y-axis")
                            humveeY = (float) rotationSpeed * (float)0.01;
                        if (rotationAxis == "z-axis")
                            humveeZ = (float) rotationSpeed * (float)0.01;
                        humveeRotation = humveeRotation * Matrix.CreateFromAxisAngle(new Vector3(1, 0, 0), humveeX) * Matrix.CreateFromAxisAngle(new Vector3(0, 1, 0), humveeY) * Matrix.CreateFromAxisAngle(new Vector3(0, 0, 1), humveeZ);
                    }
                    else if (label == "Gears")
                    {
                        if (rotationAxis == "x-axis")
                            gearsX = (float) rotationSpeed * (float)0.01;
                        if (rotationAxis == "y-axis")
                            gearsY = (float) rotationSpeed * (float)0.01;
                        if (rotationAxis == "z-axis")
                            gearsZ = (float) rotationSpeed * (float)0.01;
                        gearsRotation = gearsRotation * Matrix.CreateFromAxisAngle(new Vector3(1, 0, 0), gearsX) * Matrix.CreateFromAxisAngle(new Vector3(0, 1, 0), gearsY) * Matrix.CreateFromAxisAngle(new Vector3(0, 0, 1), gearsZ);
                    }
                    else if (label == "Cup")
                    {
                        if (rotationAxis == "x-axis")
                            cupX = (float) rotationSpeed * (float)0.01;
                        if (rotationAxis == "y-axis")
                            cupY = (float) rotationSpeed * (float)0.01;
                        if (rotationAxis == "z-axis")
                            cupZ = (float) rotationSpeed * (float)0.01;
                        cupRotation = cupRotation * Matrix.CreateFromAxisAngle(new Vector3(1, 0, 0), cupX) * Matrix.CreateFromAxisAngle(new Vector3(0, 1, 0), cupY) * Matrix.CreateFromAxisAngle(new Vector3(0, 0, 1), cupZ);
                    }
                    else if (label == "G36C Gun")
                    {
                        if (rotationAxis == "x-axis")
                            g36cX = (float) rotationSpeed * (float)0.01;
                        if (rotationAxis == "y-axis")
                            g36cY = (float) rotationSpeed * (float)0.01;
                        if (rotationAxis == "z-axis")
                            g36cZ = (float) rotationSpeed * (float)0.01;
                        g36cRotation = g36cRotation * Matrix.CreateFromAxisAngle(new Vector3(1, 0, 0), g36cX) * Matrix.CreateFromAxisAngle(new Vector3(0, 1, 0), g36cY) * Matrix.CreateFromAxisAngle(new Vector3(0, 0, 1), g36cZ);
                    }
                }
                if (rotationDirection == "Counterclockwise")
                {
                    if (label == "Humvee")
                    {
                        if (rotationAxis == "x-axis")
                            humveeX = rotationSpeed * (float) -0.01;
                        if (rotationAxis == "y-axis")
                            humveeY = rotationSpeed * (float) -0.01;
                        if (rotationAxis == "z-axis")
                            humveeZ = rotationSpeed * (float) -0.01;
                        humveeRotation = humveeRotation * Matrix.CreateFromAxisAngle(new Vector3(1, 0, 0), humveeX) * Matrix.CreateFromAxisAngle(new Vector3(0, 1, 0), humveeY) * Matrix.CreateFromAxisAngle(new Vector3(0, 0, 1), humveeZ);
                    }
                    else if (label == "Gears")
                    {
                        if (rotationAxis == "x-axis")
                            gearsX = rotationSpeed * (float) -0.01;
                        if (rotationAxis == "y-axis")
                            gearsY = rotationSpeed * (float) -0.01;
                        if (rotationAxis == "z-axis")
                            gearsZ = rotationSpeed * (float) -0.01;
                        gearsRotation = gearsRotation * Matrix.CreateFromAxisAngle(new Vector3(1, 0, 0), gearsX) * Matrix.CreateFromAxisAngle(new Vector3(0, 1, 0), gearsY) * Matrix.CreateFromAxisAngle(new Vector3(0, 0, 1), gearsZ);
                    }
                    else if (label == "Cup")
                    {
                        if (rotationAxis == "x-axis")
                            cupX = rotationSpeed * (float) -0.01;
                        if (rotationAxis == "y-axis")
                            cupY = rotationSpeed * (float) -0.01;
                        if (rotationAxis == "z-axis")
                            cupZ = rotationSpeed * (float) -0.01;
                        cupRotation = cupRotation * Matrix.CreateFromAxisAngle(new Vector3(1, 0, 0), cupX) * Matrix.CreateFromAxisAngle(new Vector3(0, 1, 0), cupY) * Matrix.CreateFromAxisAngle(new Vector3(0, 0, 1), cupZ);
                    }
                    else if (label == "G36C Gun")
                    {
                        if (rotationAxis == "x-axis")
                            g36cX = rotationSpeed * (float) -0.01;
                        if (rotationAxis == "y-axis")
                            g36cY = rotationSpeed * (float) -0.01;
                        if (rotationAxis == "z-axis")
                            g36cZ = rotationSpeed * (float) -0.01;
                        g36cRotation = g36cRotation * Matrix.CreateFromAxisAngle(new Vector3(1, 0, 0), g36cX) * Matrix.CreateFromAxisAngle(new Vector3(0, 1, 0), g36cY) * Matrix.CreateFromAxisAngle(new Vector3(0, 0, 1), g36cZ);
                    }
                }
            }

            if (scaleFlag)
            {
                if (groundMarkerNode.MarkerFound)
                {
                    if (toolbarMarkerNode.MarkerFound)
                    {
                        if (label == "Humvee")
                        {
                            if (toolbarMarkerNode.WorldTransformation.Translation.Y > 0)
                                humveeSize = toolbarMarkerNode.WorldTransformation.Translation.Y * (float)0.01;
                            ((NewtonPhysics)scene.PhysicsEngine).SetTransform(humvee.Physics, Matrix.CreateScale(humveeSize) * humveeRotation * humveeTrans * humveeMatrix);
                        }
                        if (label == "Gears")
                        {
                            if (toolbarMarkerNode.WorldTransformation.Translation.Y > 0)
                                gearsSize = toolbarMarkerNode.WorldTransformation.Translation.Y * (float)0.01;
                            ((NewtonPhysics)scene.PhysicsEngine).SetTransform(gears.Physics, Matrix.CreateScale(gearsSize) * gearsRotation * gearsTrans * gearsMatrix);
                        }
                        if (label == "Cup")
                        {
                            if (toolbarMarkerNode.WorldTransformation.Translation.Y > 0)
                                cupSize = toolbarMarkerNode.WorldTransformation.Translation.Y * (float)0.01;
                            ((NewtonPhysics)scene.PhysicsEngine).SetTransform(cup.Physics, Matrix.CreateScale(cupSize) * cupRotation * cupTrans * cupMatrix);
                        }
                        if (label == "G36C Gun")
                        {
                            if (toolbarMarkerNode.WorldTransformation.Translation.Y > 0)
                                g36cSize = toolbarMarkerNode.WorldTransformation.Translation.Y * (float)0.01;
                            ((NewtonPhysics)scene.PhysicsEngine).SetTransform(g36c.Physics, Matrix.CreateScale(g36cSize) * g36cRotation * g36cTrans * g36cMatrix);
                        }
                    }
                }
            }

            if (rotationObjectFlag)
            {
                if (groundMarkerNode.MarkerFound)
                {
                    if (toolbarMarkerNode.MarkerFound)
                    {
                        if (label == "Humvee")
                        {
                            humveeRotation = Matrix.CreateFromAxisAngle(new Vector3(1, 0, 0), MathHelper.ToRadians(toolbarMarkerNode.WorldTransformation.Translation.Y * 6)) * Matrix.CreateFromAxisAngle(new Vector3(0, 1, 0), MathHelper.ToRadians(toolbarMarkerNode.WorldTransformation.Translation.Z * 6)) * Matrix.CreateFromAxisAngle(new Vector3(0, 0, 1), MathHelper.ToRadians(toolbarMarkerNode.WorldTransformation.Translation.X * 6));
                            ((NewtonPhysics)scene.PhysicsEngine).SetTransform(humvee.Physics, Matrix.CreateScale(humveeSize) * humveeRotation * humveeTrans * humveeMatrix);
                        }
                        if (label == "Gears")
                        {
                            gearsRotation = Matrix.CreateFromAxisAngle(new Vector3(1, 0, 0), MathHelper.ToRadians(toolbarMarkerNode.WorldTransformation.Translation.Y * 6)) * Matrix.CreateFromAxisAngle(new Vector3(0, 1, 0), MathHelper.ToRadians(toolbarMarkerNode.WorldTransformation.Translation.Z * 6)) * Matrix.CreateFromAxisAngle(new Vector3(0, 0, 1), MathHelper.ToRadians(toolbarMarkerNode.WorldTransformation.Translation.X * 6));
                            ((NewtonPhysics)scene.PhysicsEngine).SetTransform(gears.Physics, Matrix.CreateScale(gearsSize) * gearsRotation * gearsTrans * gearsMatrix);
                        }
                        if (label == "Cup")
                        {
                            cupRotation = Matrix.CreateFromAxisAngle(new Vector3(1, 0, 0), MathHelper.ToRadians(toolbarMarkerNode.WorldTransformation.Translation.Y * 6)) * Matrix.CreateFromAxisAngle(new Vector3(0, 1, 0), MathHelper.ToRadians(toolbarMarkerNode.WorldTransformation.Translation.Z * 6)) * Matrix.CreateFromAxisAngle(new Vector3(0, 0, 1), MathHelper.ToRadians(toolbarMarkerNode.WorldTransformation.Translation.X * 6));
                            ((NewtonPhysics)scene.PhysicsEngine).SetTransform(cup.Physics, Matrix.CreateScale(cupSize) * cupRotation * cupTrans * cupMatrix);
                        }
                        if (label == "G36C Gun")
                        {
                            g36cRotation = Matrix.CreateFromAxisAngle(new Vector3(1, 0, 0), MathHelper.ToRadians(toolbarMarkerNode.WorldTransformation.Translation.Y * 6)) * Matrix.CreateFromAxisAngle(new Vector3(0, 1, 0), MathHelper.ToRadians(toolbarMarkerNode.WorldTransformation.Translation.Z * 6)) * Matrix.CreateFromAxisAngle(new Vector3(0, 0, 1), MathHelper.ToRadians(toolbarMarkerNode.WorldTransformation.Translation.X * 6));
                            ((NewtonPhysics)scene.PhysicsEngine).SetTransform(g36c.Physics, Matrix.CreateScale(g36cSize) * g36cRotation * g36cTrans * g36cMatrix);
                        }
                    }
                }
            }

            if (translationModeFlag)
            {
                // If ground marker array is detected
                if (groundMarkerNode.MarkerFound)
                {
                    if (toolbarMarkerNode.MarkerFound)
                    {
                        if (label == "Humvee")
                        {
                            humveeMatrix = Matrix.CreateTranslation(toolbarMarkerNode.WorldTransformation.Translation) * Matrix.Invert(groundMarkerNode.WorldTransformation);
                            // Modify the transformation in the physics engine
                            ((NewtonPhysics)scene.PhysicsEngine).SetTransform(humvee.Physics, Matrix.CreateScale(humveeSize) * humveeRotation * humveeTrans * humveeMatrix);
                        }
                        if (label == "Gears")
                        {
                            gearsMatrix = Matrix.CreateTranslation(toolbarMarkerNode.WorldTransformation.Translation) * Matrix.Invert(groundMarkerNode.WorldTransformation);
                            // Modify the transformation in the physics engine
                            ((NewtonPhysics)scene.PhysicsEngine).SetTransform(gears.Physics, Matrix.CreateScale(gearsSize) * gearsRotation * gearsTrans * gearsMatrix);
                        }
                        if (label == "Cup")
                        {
                            cupMatrix = Matrix.CreateTranslation(toolbarMarkerNode.WorldTransformation.Translation) * Matrix.Invert(groundMarkerNode.WorldTransformation);
                            // Modify the transformation in the physics engine
                            ((NewtonPhysics)scene.PhysicsEngine).SetTransform(cup.Physics, Matrix.CreateScale(cupSize) * cupRotation * cupTrans * cupMatrix);
                        }
                        if (label == "G36C Gun")
                        {
                            g36cMatrix = Matrix.CreateTranslation(toolbarMarkerNode.WorldTransformation.Translation) * Matrix.Invert(groundMarkerNode.WorldTransformation);
                            // Modify the transformation in the physics engine
                            ((NewtonPhysics)scene.PhysicsEngine).SetTransform(g36c.Physics, Matrix.CreateScale(g36cSize) * g36cRotation * g36cTrans * g36cMatrix);
                        }
                    }
                }
            }
            else if (translationModeFlag == false || label == "Nothing is selected")
            {
                // If ground marker array is detected
                if (groundMarkerNode.MarkerFound)
                {
                    if (resetFlag == true)
                    {
                        UI2DRenderer.WriteText(Vector2.Zero, "System has been reset", Color.Red, textFont1, GoblinEnums.HorizontalAlignment.Right, GoblinEnums.VerticalAlignment.Top);
                    }
                    if (label == "Humvee" || label == "Gears" || label == "Cup" || label == "G36C Gun")
                        if (panelTrigger)
                            objectFrame.Visible = true;
                    // If the toolbar marker array is detected
                    if (toolbarMarkerNode.MarkerFound)
                    {
                        if (collusionflag == false)
                        {
                            // Create collision pair1 and add a collision callback function that will be called when the pair collides
                            NewtonPhysics.CollisionPair pair1 = new NewtonPhysics.CollisionPair(humvee.Physics, pointingMarker.Physics);
                            ((NewtonPhysics)scene.PhysicsEngine).AddCollisionCallback(pair1, humveeCollision);
                            // Create collision pair2 and add a collision callback function that will be called when the pair collides
                            NewtonPhysics.CollisionPair pair2 = new NewtonPhysics.CollisionPair(gears.Physics, pointingMarker.Physics);
                            ((NewtonPhysics)scene.PhysicsEngine).AddCollisionCallback(pair2, gearsCollision);
                            // Create collision pair3 and add a collision callback function that will be called when the pair collides
                            NewtonPhysics.CollisionPair pair3 = new NewtonPhysics.CollisionPair(cup.Physics, pointingMarker.Physics);
                            ((NewtonPhysics)scene.PhysicsEngine).AddCollisionCallback(pair3, cupCollision);
                            // Create collision pair4 and add a collision callback function that will be called when the pair collides
                            NewtonPhysics.CollisionPair pair4 = new NewtonPhysics.CollisionPair(g36c.Physics, pointingMarker.Physics);
                            ((NewtonPhysics)scene.PhysicsEngine).AddCollisionCallback(pair4, g36cCollision);
                            collusionflag = true;
                        }
                        if (rotationObjectFlag)
                        {
                            ((NewtonPhysics)scene.PhysicsEngine).SetTransform(pointingMarker.Physics, Matrix.CreateTranslation(-200, -200, -200));
                            pointingMarker.Material.Diffuse = new Vector4(pointingMarker.Material.Diffuse.X, pointingMarker.Material.Diffuse.Y, pointingMarker.Material.Diffuse.Z, 0);
                        }
                        else if (scaleFlag)
                        {
                            ((NewtonPhysics)scene.PhysicsEngine).SetTransform(pointingMarker.Physics, Matrix.CreateTranslation(-200, -200, -200));
                            pointingMarker.Material.Diffuse = new Vector4(pointingMarker.Material.Diffuse.X, pointingMarker.Material.Diffuse.Y, pointingMarker.Material.Diffuse.Z, 0);
                        }
                        else
                        {
                            pointingMarker.Material.Diffuse = new Vector4(pointingMarker.Material.Diffuse.X, pointingMarker.Material.Diffuse.Y, pointingMarker.Material.Diffuse.Z, 0.6f);
                            Matrix markerMatrix = Matrix.CreateScale((float)0.4) * toolbarMarkerNode.WorldTransformation * Matrix.Invert(groundMarkerNode.WorldTransformation);
                            // Modify the transformation in the physics engine
                            ((NewtonPhysics)scene.PhysicsEngine).SetTransform(pointingMarker.Physics, markerMatrix);
                        }
                    }
                    else
                    {
                        ((NewtonPhysics)scene.PhysicsEngine).SetTransform(pointingMarker.Physics, Matrix.CreateTranslation(-400, -400, -400));
                        pointingMarker.Material.Diffuse = new Vector4(pointingMarker.Material.Diffuse.X, pointingMarker.Material.Diffuse.Y, pointingMarker.Material.Diffuse.Z, 0);
                    }
                    ((NewtonPhysics)scene.PhysicsEngine).SetTransform(humvee.Physics, Matrix.CreateScale(humveeSize) * humveeRotation * humveeTrans * humveeMatrix);
                    ((NewtonPhysics)scene.PhysicsEngine).SetTransform(gears.Physics, Matrix.CreateScale(gearsSize) * gearsRotation * gearsTrans * gearsMatrix);
                    ((NewtonPhysics)scene.PhysicsEngine).SetTransform(cup.Physics, Matrix.CreateScale(cupSize) * cupRotation * cupTrans * cupMatrix);
                    ((NewtonPhysics)scene.PhysicsEngine).SetTransform(g36c.Physics, Matrix.CreateScale(g36cSize) * g36cRotation * g36cTrans * g36cMatrix);
                }
                else
                {
                    objectFrame.Visible = false;
                }
            }
            if (groundMarkerNode.MarkerFound == true && toolbarMarkerNode.MarkerFound == true && transferFlag == true)
            {
                pointingMarker.Material.Diffuse = new Vector4(pointingMarker.Material.Diffuse.X, pointingMarker.Material.Diffuse.Y, pointingMarker.Material.Diffuse.Z, 0);
                if (label == "Humvee")
                {
                    if (humveeflag == false)
                    {
                        groundMarkerNode.RemoveChild(humvee);
                        toolbarMarkerNode.AddChild(humvee);
                        humveeflag = true;
                    }
                }
                if (label == "Gears")
                {
                    if (gearsflag == false)
                    {
                        groundMarkerNode.RemoveChild(gears);
                        toolbarMarkerNode.AddChild(gears);
                        gearsflag = true;
                    }
                }
                if (label == "Cup")
                {
                    if (cupflag == false)
                    {
                        groundMarkerNode.RemoveChild(cup);
                        toolbarMarkerNode.AddChild(cup);
                        cupflag = true;
                    }
                }
                if (label == "G36C Gun")
                {
                    if (g36cflag == false)
                    {
                        groundMarkerNode.RemoveChild(g36c);
                        toolbarMarkerNode.AddChild(g36c);
                        g36cflag = true;
                    }
                }
            }
            if (groundMarkerNode.MarkerFound == true && toolbarMarkerNode.MarkerFound == false && transferFlag == true)
            {
                if (humveeflag == true)
                {
                    toolbarMarkerNode.RemoveChild(humvee);
                    // Create humvee model.
                    ModelLoader loader = new ModelLoader();
                    humvee = new GeometryNode("Humvee");
                    humvee.Model = (Model)loader.Load("", "humvee");
                    // Add this humvee model to the physics engine for collision detection
                    humvee.AddToPhysicsEngine = true;
                    humvee.Physics.Shape = ShapeType.ConvexHull;
                    // Set model materials.
                    ((Model)humvee.Model).UseInternalMaterials = true;
                    // Create collision pair1 and add a collision callback function that will be called when the pair collides
                    NewtonPhysics.CollisionPair pair1 = new NewtonPhysics.CollisionPair(humvee.Physics, pointingMarker.Physics);
                    ((NewtonPhysics)scene.PhysicsEngine).AddCollisionCallback(pair1, humveeCollision);
                    ((Model)humvee.Model).ShowBoundingBox = true;
                    groundMarkerNode.AddChild(humvee);
                    humveeflag = false;
                }
                if (gearsflag == true)
                {
                    toolbarMarkerNode.RemoveChild(gears);
                    // Create humvee model.
                    ModelLoader loader = new ModelLoader();
                    // Create gears model.
                    gears = new GeometryNode("Gears");
                    gears.Model = (Model)loader.Load("", "gears");
                    // Add this gears model to the physics engine for collision detection
                    gears.AddToPhysicsEngine = true;
                    gears.Physics.Shape = ShapeType.ConvexHull;
                    // Set model materials.
                    ((Model)gears.Model).UseInternalMaterials = true;
                    // Create collision pair2 and add a collision callback function that will be called when the pair collides
                    NewtonPhysics.CollisionPair pair2 = new NewtonPhysics.CollisionPair(gears.Physics, pointingMarker.Physics);
                    ((NewtonPhysics)scene.PhysicsEngine).AddCollisionCallback(pair2, gearsCollision);
                    ((Model)gears.Model).ShowBoundingBox = true;
                    groundMarkerNode.AddChild(gears);
                    gearsflag = false;
                }
                if (cupflag == true)
                {
                    toolbarMarkerNode.RemoveChild(cup);
                    // Create humvee model.
                    ModelLoader loader = new ModelLoader();
                    // Create cup model.
                    cup = new GeometryNode("Cup");
                    cup.Model = (Model)loader.Load("", "bardak");
                    // Add this cup model to the physics engine for collision detection
                    cup.AddToPhysicsEngine = true;
                    cup.Physics.Shape = ShapeType.ConvexHull;
                    // Set model materials.
                    ((Model)cup.Model).UseInternalMaterials = true;
                    // Create collision pair3 and add a collision callback function that will be called when the pair collides
                    NewtonPhysics.CollisionPair pair3 = new NewtonPhysics.CollisionPair(cup.Physics, pointingMarker.Physics);
                    ((NewtonPhysics)scene.PhysicsEngine).AddCollisionCallback(pair3, cupCollision);
                    ((Model)cup.Model).ShowBoundingBox = true;
                    groundMarkerNode.AddChild(cup);
                    cupflag = false;
                }
                if (g36cflag == true)
                {
                    toolbarMarkerNode.RemoveChild(g36c);
                    // Create humvee model.
                    ModelLoader loader = new ModelLoader();
                    // Create g36c model.
                    g36c = new GeometryNode("G36C Gun");
                    g36c.Model = (Model)loader.Load("", "g36c");
                    // Add this g36c model to the physics engine for collision detection
                    g36c.AddToPhysicsEngine = true;
                    g36c.Physics.Shape = ShapeType.ConvexHull;
                    // Set model materials.
                    ((Model)g36c.Model).UseInternalMaterials = true;
                    // Create collision pair4 and add a collision callback function that will be called when the pair collides
                    NewtonPhysics.CollisionPair pair4 = new NewtonPhysics.CollisionPair(g36c.Physics, pointingMarker.Physics);
                    ((NewtonPhysics)scene.PhysicsEngine).AddCollisionCallback(pair4, g36cCollision);
                    ((Model)g36c.Model).ShowBoundingBox = true;
                    groundMarkerNode.AddChild(g36c);
                    g36cflag = false;
                }
            }
        }

        private void displaySystemPanel()
        {
            //Displaying system control panels.
            if(label == "Nothing is selected")
                UI2DRenderer.WriteText(Vector2.Zero, label, Color.Red, textFont1, GoblinEnums.HorizontalAlignment.Center, GoblinEnums.VerticalAlignment.Top);
            else
                UI2DRenderer.WriteText(Vector2.Zero, label + " is selected", Color.Red, textFont1, GoblinEnums.HorizontalAlignment.Center, GoblinEnums.VerticalAlignment.Top);
            if(translationModeFlag)
                UI2DRenderer.WriteText(Vector2.Zero, "Transformation Mode", Color.Red, textFont1, GoblinEnums.HorizontalAlignment.Right, GoblinEnums.VerticalAlignment.Top);
            if(transferFlag)
                UI2DRenderer.WriteText(Vector2.Zero, "Transfer Mode", Color.Red, textFont1, GoblinEnums.HorizontalAlignment.Right, GoblinEnums.VerticalAlignment.Top);
            if(rotationObjectFlag)
                UI2DRenderer.WriteText(Vector2.Zero, "Rotation Mode", Color.Red, textFont1, GoblinEnums.HorizontalAlignment.Right, GoblinEnums.VerticalAlignment.Top);
            if(scaleFlag)
                UI2DRenderer.WriteText(Vector2.Zero, "Scale Mode", Color.Red, textFont1, GoblinEnums.HorizontalAlignment.Right, GoblinEnums.VerticalAlignment.Top);
            if(panelTrigger)
                UI2DRenderer.WriteText(Vector2.Zero, "Extra Work - Panel Control", Color.Red, textFont1, GoblinEnums.HorizontalAlignment.Right, GoblinEnums.VerticalAlignment.Top);
            //Control panels for attaching camera to planets and objects
            UI2DRenderer.WriteText(new Vector2(5, 20), "Keys Control Menu", Color.Red, textFont1, Vector2.One * 0.8f);
            UI2DRenderer.WriteText(new Vector2(5, 35), "'Up Arrow' - Scale Up a selected object", Color.Red, textFont1, Vector2.One * 0.8f);
            UI2DRenderer.WriteText(new Vector2(5, 50), "'Down Arrow' - Scale Down a selected object", Color.Red, textFont1, Vector2.One * 0.8f);
            UI2DRenderer.WriteText(new Vector2(5, 65), "'S' - Scale Mode", Color.Red, textFont1, Vector2.One * 0.8f);
            UI2DRenderer.WriteText(new Vector2(5, 80), "'C' - Rotation Mode", Color.Red, textFont1, Vector2.One * 0.8f);
            UI2DRenderer.WriteText(new Vector2(5, 95), "'T' - Transformation Mode", Color.Red, textFont1, Vector2.One * 0.8f);
            UI2DRenderer.WriteText(new Vector2(5, 110), "'E' - Extra Work: Panel Control", Color.Red, textFont1, Vector2.One * 0.8f);
            UI2DRenderer.WriteText(new Vector2(5, 125), "'M' - Transfer Mode", Color.Red, textFont1, Vector2.One * 0.8f);
            UI2DRenderer.WriteText(new Vector2(5, 140), "'Q' - Quit Mode", Color.Red, textFont1, Vector2.One * 0.8f);
            UI2DRenderer.WriteText(new Vector2(5, 155), "'R' - Reset System", Color.Red, textFont1, Vector2.One * 0.8f);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            markerHelper(gameTime);
            displaySystemPanel();
            scene.Draw(gameTime.ElapsedGameTime, gameTime.IsRunningSlowly);
        }
    }
} 