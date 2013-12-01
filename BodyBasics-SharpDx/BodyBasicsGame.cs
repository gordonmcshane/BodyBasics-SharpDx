using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BodyBasicsSharpDx.Extensions;
using BodyBasicsSharpDx.Properties;
using Microsoft.Kinect;
using SharpDX;
using SharpDX.Toolkit;
using SharpDX.Toolkit.Graphics;

namespace BodyBasicsSharpDx
{
    public class BodyBasicsGame : Game
    {
        private const float HandRadius = 30;
        private const float JointRadius = 5;

        private SpriteFont _arial16BMFont;
        private Body[] _bodies;

        private CircleRenderer _circleRenderer;
        private CoordinateMapper _coordinateMapper;

        private uint _framesSinceUpdate;

        private GraphicsDeviceManager _graphicsDeviceManager;

        private KinectSensor _kinectSensor;
        private DateTime _nextStatusUpdate = DateTime.MinValue;

        private BodyFrameReader _reader;

        private SpriteBatch _spriteBatch;
        private long _startTime;

        private string _statusText;
        private Stopwatch _stopwatch;

        public BodyBasicsGame()
        {
            _graphicsDeviceManager = new GraphicsDeviceManager(this);
            
            Content.RootDirectory = "Content";
        }

        protected override void Initialize()
        {
            base.Initialize();

            _kinectSensor = KinectSensor.Default;

            _stopwatch = new Stopwatch();

            if (_kinectSensor != null)
            {
                // get the coordinate mapper
                _coordinateMapper = _kinectSensor.CoordinateMapper;

                // open the sensor
                _kinectSensor.Open();

                // get the depth (display) extents
                FrameDescription frameDescription = _kinectSensor.DepthFrameSource.FrameDescription;

                _bodies = new Body[_kinectSensor.BodyFrameSource.BodyCount];

                // open the reader for the body frames
                _reader = _kinectSensor.BodyFrameSource.OpenReader();

                // set the status text
                _statusText = Resources.InitializingStatusTextFormat;
            }
            else
            {
                // on failure, set the status text
                _statusText = Resources.NoSensorStatusText;
            }

            
        }

        protected override void LoadContent()
        {
            base.LoadContent();

            if (_reader != null)
            {
                _reader.FrameArrived += Reader_FrameArrived;
            }

            _arial16BMFont = Content.Load<SpriteFont>("Arial16");

            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _circleRenderer = new CircleRenderer(GraphicsDevice, 32);
        }

        protected override void UnloadContent()
        {
            base.UnloadContent();

            if (_reader != null)
            {
                // BodyFrameReder is IDisposable
                _reader.Dispose();
                _reader = null;
            }

            // Body is IDisposable
            if (_bodies != null)
            {
                foreach (Body body in _bodies)
                {
                    if (body != null)
                    {
                        body.Dispose();
                    }
                }
            }

            if (_kinectSensor != null)
            {
                _kinectSensor.Close();
                _kinectSensor = null;
            }

            _spriteBatch.Dispose();
            _circleRenderer.Dispose();
        }

        private void Reader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            BodyFrameReference frameReference = e.FrameReference;

            if (_startTime == 0)
            {
                _startTime = frameReference.RelativeTime;
            }

            try
            {
                BodyFrame frame = frameReference.AcquireFrame();

                if (frame != null)
                {
                    // BodyFrame is IDisposable
                    using (frame)
                    {
                        _framesSinceUpdate++;

                        // update status unless last message is sticky for a while
                        if (DateTime.Now >= _nextStatusUpdate)
                        {
                            // calcuate fps based on last frame received
                            double fps = 0.0;

                            if (_stopwatch.IsRunning)
                            {
                                _stopwatch.Stop();
                                fps = _framesSinceUpdate/_stopwatch.Elapsed.TotalSeconds;
                                _stopwatch.Reset();
                            }

                            _nextStatusUpdate = DateTime.Now + TimeSpan.FromSeconds(1);
                            _statusText = string.Format(Resources.StandardStatusTextFormat, fps,
                                frameReference.RelativeTime - _startTime);
                        }

                        if (!_stopwatch.IsRunning)
                        {
                            _framesSinceUpdate = 0;
                            _stopwatch.Start();
                        }

                        frame.GetAndRefreshBodyData(_bodies);
                    }
                }
            }
            catch (Exception)
            {
                // ignore if the frame is no longer available
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            GraphicsDevice.Clear(Color.Black);

            foreach (Body body in _bodies.Where(b => b != null))
            {
                if (body.IsTracked)
                {
                    IReadOnlyDictionary<JointType, Joint> joints = body.Joints;

                    // convert the joint points to depth (display) space
                    var jointPoints = new Dictionary<JointType, Vector2>();
                    foreach (JointType jointType in joints.Keys)
                    {
                        DepthSpacePoint depthSpacePoint =
                            _coordinateMapper.MapCameraPointToDepthSpace(joints[jointType].Position);
                        jointPoints[jointType] = new Vector2(depthSpacePoint.X, depthSpacePoint.Y);
                    }

                    DrawBody(joints, jointPoints, _spriteBatch);

                    DrawHand(body.HandLeftState, jointPoints[JointType.HandLeft], _spriteBatch);
                    DrawHand(body.HandRightState, jointPoints[JointType.HandRight], _spriteBatch);
                }
            }

            _spriteBatch.Begin();
            _spriteBatch.DrawString(_arial16BMFont, _statusText, new Vector2(5, 5), Color.White);
            _spriteBatch.End();
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            _circleRenderer.Update();
        }

        private void DrawBody(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Vector2> jointPoints,
            SpriteBatch spriteBatch)
        {
            // Draw the bones
            _spriteBatch.Begin();

            // Torso
            DrawBone(joints, jointPoints, JointType.Head, JointType.Neck, spriteBatch);
            DrawBone(joints, jointPoints, JointType.Neck, JointType.SpineShoulder, spriteBatch);
            DrawBone(joints, jointPoints, JointType.SpineShoulder, JointType.SpineMid, spriteBatch);
            DrawBone(joints, jointPoints, JointType.SpineMid, JointType.SpineBase, spriteBatch);
            DrawBone(joints, jointPoints, JointType.SpineShoulder, JointType.ShoulderRight, spriteBatch);
            DrawBone(joints, jointPoints, JointType.SpineShoulder, JointType.ShoulderLeft, spriteBatch);
            DrawBone(joints, jointPoints, JointType.SpineBase, JointType.HipRight, spriteBatch);
            DrawBone(joints, jointPoints, JointType.SpineBase, JointType.HipLeft, spriteBatch);

            // Right Arm    
            DrawBone(joints, jointPoints, JointType.ShoulderRight, JointType.ElbowRight, spriteBatch);
            DrawBone(joints, jointPoints, JointType.ElbowRight, JointType.WristRight, spriteBatch);
            DrawBone(joints, jointPoints, JointType.WristRight, JointType.HandRight, spriteBatch);
            DrawBone(joints, jointPoints, JointType.HandRight, JointType.HandTipRight, spriteBatch);
            DrawBone(joints, jointPoints, JointType.WristRight, JointType.ThumbRight, spriteBatch);

            // Left Arm
            DrawBone(joints, jointPoints, JointType.ShoulderLeft, JointType.ElbowLeft, spriteBatch);
            DrawBone(joints, jointPoints, JointType.ElbowLeft, JointType.WristLeft, spriteBatch);
            DrawBone(joints, jointPoints, JointType.WristLeft, JointType.HandLeft, spriteBatch);
            DrawBone(joints, jointPoints, JointType.HandLeft, JointType.HandTipLeft, spriteBatch);
            DrawBone(joints, jointPoints, JointType.WristLeft, JointType.ThumbLeft, spriteBatch);

            // Right Leg
            DrawBone(joints, jointPoints, JointType.HipRight, JointType.KneeRight, spriteBatch);
            DrawBone(joints, jointPoints, JointType.KneeRight, JointType.AnkleRight, spriteBatch);
            DrawBone(joints, jointPoints, JointType.AnkleRight, JointType.FootRight, spriteBatch);

            // Left Leg
            DrawBone(joints, jointPoints, JointType.HipLeft, JointType.KneeLeft, spriteBatch);
            DrawBone(joints, jointPoints, JointType.KneeLeft, JointType.AnkleLeft, spriteBatch);
            DrawBone(joints, jointPoints, JointType.AnkleLeft, JointType.FootLeft, spriteBatch);

            _spriteBatch.End();

            // Draw the joints
            foreach (JointType jointType in joints.Keys)
            {
                TrackingState trackingState = joints[jointType].TrackingState;
                _circleRenderer.Draw(jointPoints[jointType].X, jointPoints[jointType].Y, JointRadius,
                    trackingState == TrackingState.Tracked ? Color.Blue : Color.DarkBlue);
            }
        }

        private void DrawBone(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Vector2> jointPoints,
            JointType jointType0, JointType jointType1, SpriteBatch spriteBatch)
        {
            Joint joint0 = joints[jointType0];
            Joint joint1 = joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == TrackingState.NotTracked ||
                joint1.TrackingState == TrackingState.NotTracked)
            {
                return;
            }

            // Don't draw if both points are inferred
            if (joint0.TrackingState == TrackingState.Inferred &&
                joint1.TrackingState == TrackingState.Inferred)
            {
                return;
            }

            Color color = Color.Gray;
            int width = 1;
            // We assume all drawn bones are inferred unless BOTH joints are tracked
            if ((joint0.TrackingState == TrackingState.Tracked) && (joint1.TrackingState == TrackingState.Tracked))
            {
                color = Color.CornflowerBlue;
                width = 6;
            }

            spriteBatch.DrawLine(jointPoints[jointType0], jointPoints[jointType1], color, width);
        }

        private void DrawHand(HandState handState, Vector2 handPosition, SpriteBatch spriteBatch)
        {
            switch (handState)
            {
                case HandState.Closed:
                    _circleRenderer.Draw(handPosition.X, handPosition.Y, HandRadius, Color.Red);
                    break;

                case HandState.Open:
                    _circleRenderer.Draw(handPosition.X, handPosition.Y, HandRadius, Color.Green);
                    break;

                case HandState.Lasso:
                    _circleRenderer.Draw(handPosition.X, handPosition.Y, HandRadius, Color.Purple);
                    break;
            }
        }
    }
}