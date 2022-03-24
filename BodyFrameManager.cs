// .Net API
using System;
using System.Collections.Generic;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
// Kinect V2 API
using Microsoft.Kinect;
using Microsoft.Speech.AudioFormat;
using Microsoft.Speech.Recognition;

namespace NUI3D
{
    public class BodyFrameManager
    {
        /// Borrowed from T11_VoiceControl and make some changes
        private KinectSensor sensor;
        private RecognizerInfo kinectRecognizerInfo;
        private SpeechRecognitionEngine recognizer;

        private Body[] bodies;

        private Boolean mapToColorSpace = true; 
        private bool retry = false;
        private int progressValue = 0;
        private int[] foodEaten = new int[8] { 0, 0, 0, 0, 0 ,0 ,0 ,0 };

        private TextBlock win, lose;
        private TextBlock recognizedCommand;
        private SoundPlayer eat = new SoundPlayer("audio/eat.wav");
        private SoundPlayer vomit = new SoundPlayer("audio/vomit.wav");
        private SoundPlayer throwFood = new SoundPlayer("audio/throw.wav");

        private Image gameCover;

        private BitmapImage playerCharacter, parentCharacter;
        private BitmapImage yes, no;
        private BitmapImage egg, apple, beef, broccoli, pudding, chickenThigh, fish, mushroom;
        private void LoadImages() 
        {
            playerCharacter = new BitmapImage(
                new Uri("Images/kid.png", UriKind.Relative));
            parentCharacter = new BitmapImage(
                new Uri("Images/Parents.png", UriKind.Relative));
            yes = new BitmapImage(
                new Uri("Images/yes.png", UriKind.Relative));
            no = new BitmapImage(
                new Uri("Images/no.png", UriKind.Relative));
            egg = new BitmapImage(
                new Uri("Images/egg.png", UriKind.Relative));
            apple = new BitmapImage(
                new Uri("Images/apple.png", UriKind.Relative));
            beef = new BitmapImage(
                new Uri("Images/beef.png", UriKind.Relative));
            broccoli = new BitmapImage(
                new Uri("Images/broccoli.png", UriKind.Relative));
            pudding = new BitmapImage(
                new Uri("Images/pudding.png", UriKind.Relative));
            chickenThigh = new BitmapImage(
                new Uri("Images/chickenThigh.png", UriKind.Relative));
            fish = new BitmapImage(
                new Uri("Images/fish.png", UriKind.Relative));
            mushroom = new BitmapImage(
                new Uri("Images/mushroom.png", UriKind.Relative));
        }
        // Borrowed from T11_VoiceControl and make some changes

        // Borrowed from T11_VoiceControl and make some changes
        private RecognizerInfo FindKinectRecognizerInfo()
        {
            var recognizers =
                SpeechRecognitionEngine.InstalledRecognizers();

            foreach (RecognizerInfo recInfo in recognizers)
            {
                // look at each recognizer info value 
                // to find the one that works for Kinect
                if (recInfo.AdditionalInfo.ContainsKey("Kinect"))
                {
                    string details = recInfo.AdditionalInfo["Kinect"];
                    if (details == "True"
            && recInfo.Culture.Name == "en-US")
                    {
                        // If we get here we have found 
                        // the info we want to use
                        return recInfo;
                    }
                }
            }
            return null;
        }

        private Choices commands = new Choices();
        private String[] names = { "yes", "no", "retry" };

        Dictionary<int, string> goalItem = new Dictionary<int, string>();
        private void BuildCommands() // call it in Window_Loaded()
        {
            commands.Add(names);
            goalItem.Add(0, "egg");
            goalItem.Add(1, "apple");
            goalItem.Add(2, "beef");
            goalItem.Add(3, "broccoli");
            goalItem.Add(4, "pudding");
            goalItem.Add(5, "chickenThigh");
            goalItem.Add(6, "fish");
            goalItem.Add(7, "mushroom");
        }

        // Borrowed from T11_VoiceControl
        private void BuildGrammar() // call it Window_Loaded()
        {
            GrammarBuilder grammarBuilder = new GrammarBuilder(commands);

            // the same culture as the recognizer (US English)
            grammarBuilder.Culture = kinectRecognizerInfo.Culture;

            Grammar grammar = new Grammar(grammarBuilder);

            recognizer.LoadGrammar(grammar);
        }
        // Borrowed from T11_VoiceControl and make some changes
        private void Recognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {

            // class exercise 1
            if (e.Result.Confidence < 0.1) return;


            switch (e.Result.Text.ToLower())
            {
                case "yes":
                    yesExist = true;
                    break;
                case "no":
                    noExist = true;
                    break;
                case "retry":
                    retry = true;
                    break;
            }

        }

        public void Init(KinectSensor s, Image wpfImageForDisplay, TextBlock commands, TextBlock gameWin, TextBlock gameLose, Image cover, Boolean toColorSpace = true)
        {
            sensor = s;

            win = gameWin;
            lose = gameLose;
            gameCover = cover;
            
            recognizedCommand = commands;

            LoadImages();

            kinectRecognizerInfo = FindKinectRecognizerInfo();
            if (kinectRecognizerInfo != null)
            {
                recognizer = new SpeechRecognitionEngine(kinectRecognizerInfo);
            }

            BuildCommands();

            BuildGrammar();

            IReadOnlyList<AudioBeam> audioBeamList = sensor.AudioSource.AudioBeams;
            System.IO.Stream audioStream = audioBeamList[0].OpenInputStream();

            KinectAudioStream kinectAudioStream = new KinectAudioStream(audioStream);
            // let the convertStream know speech is going active
            kinectAudioStream.SpeechActive = true;

            this.recognizer.SetInputToAudioStream(
                kinectAudioStream, new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));

            // recognize words repeatedly and asynchronously
            recognizer.RecognizeAsync(RecognizeMode.Multiple);
            recognizer.SpeechRecognized += Recognizer_SpeechRecognized;

            if (toColorSpace) // map the skeleton to the color space
            {
                drawingImgWidth = sensor.ColorFrameSource.FrameDescription.Width;
                drawingImgHeight = sensor.ColorFrameSource.FrameDescription.Height;
            } else // map the skeleton to the depth space 
            {
                drawingImgWidth = sensor.DepthFrameSource.FrameDescription.Width;
                drawingImgHeight = sensor.DepthFrameSource.FrameDescription.Height;
            }

            DrawingGroupInit(wpfImageForDisplay);

            mapToColorSpace = toColorSpace; 

            BodyFrameReaderInit();

        }
        // Borrowed from T11_VoiceControl
        public Point MapCameraPointToScreenSpace(Body body, JointType jointType)
        {
            Point screenPt = new Point(0, 0);
            if (mapToColorSpace) // to color space 
            {
                ColorSpacePoint pt = sensor.CoordinateMapper.MapCameraPointToColorSpace(
                body.Joints[jointType].Position);
                screenPt.X = pt.X;
                screenPt.Y = pt.Y;
            }                
            else // to depth space
            {
                DepthSpacePoint pt = sensor.CoordinateMapper.MapCameraPointToDepthSpace(
                    body.Joints[jointType].Position);
                screenPt.X = pt.X;
                screenPt.Y = pt.Y;
            }
            return screenPt;
        }
        // Borrowed from T11_VoiceControl
        private void BodyFrameReaderInit()
        {
            BodyFrameReader bodyFrameReader = sensor.BodyFrameSource.OpenReader();
            bodyFrameReader.FrameArrived += BodyFrameReader_FrameArrived; ;

            // BodyCount: maximum number of bodies that can be tracked at one time
            bodies = new Body[sensor.BodyFrameSource.BodyCount];
        }

        void Reset()
        {
            for (int i = 0; i < maxNum; ++i)
            {
                foodEaten[i] = 0;
            }
            lose.Visibility = Visibility.Hidden;
            win.Visibility = Visibility.Hidden;
            progressValue = 0;
            chance = 3;
            yes_Y = 900;
            yes_X = 0;
            no_Y = 0;
            no_X = 900;
            score = 0;
            target_Y = 50;
            targetExist = false;
            retry = false;
        }
        // Borrowed from T11_VoiceControl and make some changes
        private void BodyFrameReader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            // when the game is continuing
            if ((progressValue*12.5 < 99) && (chance > 0))
            {
                using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
                {
                    if (bodyFrame == null) return;

                    bodyFrame.GetAndRefreshBodyData(bodies);

                    using (DrawingContext dc = drawingGroup.Open())
                    {
                        // draw a transparent background to set the render size
                        dc.DrawRectangle(Brushes.Transparent, null,
                                new Rect(0.0, 0.0, drawingImgWidth, drawingImgHeight));

                        foreach (Body body in bodies)
                        {
                            if (body.IsTracked)
                            {
                                gameCover.Visibility = Visibility.Hidden;
                                // draw a skeleton
                                DrawSkeleton(body, dc);
                            }
                        }
                    }
                }
            }
            else
            {
                // When player has no more chance
                if (chance == 0)
                {
                    lose.Visibility = Visibility.Visible;
                }
                // When player win the game
                if (progressValue * 12.5 == 100)
                {
                    win.Visibility = Visibility.Visible;
                }
                // reset the game data
                if (retry)
                {
                    Reset();
                }
            }
        }
        // Borrowed from T11_VoiceControl
        private DrawingGroup drawingGroup;
        private DrawingImage drawingImg;
        private double drawingImgWidth = 1920, drawingImgHeight = 1080;
        private void DrawingGroupInit(Image wpfImageForDisplay) // called in Window_Loaded 
        {
            drawingGroup = new DrawingGroup();
            drawingImg = new DrawingImage(drawingGroup);
            wpfImageForDisplay.Source = drawingImg;

            // prevent drawing outside of our render area
            drawingGroup.ClipGeometry = new RectangleGeometry(
                                        new Rect(0.0, 0.0, drawingImgWidth, drawingImgHeight));
        }
        // Borrowed from T11_VoiceControl and make some changes
        private void DrawSkeleton(Body body, DrawingContext dc)
        {
            foreach (JointType jt in body.Joints.Keys)
            {
                Point pt = MapCameraPointToScreenSpace(body, jt);
                if (jt == JointType.Head)
                {
                    HitTest();
                    DisplayUI();
                    DrawParent(dc);
                    DrawItems(dc, pt);
                    DrawYesAndNo(dc, pt);
                    dc.DrawImage(playerCharacter, new Rect(pt.X, 900, 200, 200));
                }
            }
        }

        private void DisplayUI()
        {
            int count = 0;
            for (int i = 0; i < maxNum; ++i)
            {
                if (foodEaten[i] == 1)
                    count += 1;
            }
            progressValue = count;
            recognizedCommand.Text = "Target Food: " + goalItem[targetId] + " Progress: "
                + progressValue * 12.5 + "%" + "                                                                             Score: " + score
                + " Number of Chances to fail: " + chance;
        }

        // my main contribution
        private int yes_Y = 900;
        private double yes_X = 0;
        private int no_Y = 0;
        private double no_X = 900;
        private bool yesExist = false;
        private bool noExist = false;
        private void DrawYesAndNo(DrawingContext dc, Point pt)
        {
            // when yes and no don't exist
            // set the position of yes and no to near the player position
            if (!yesExist)
                yes_X = pt.X + 75;
            if (!noExist)
                no_X = pt.X + 75;

            // if yes and no exist
            // change their position y and draw them out
            if (yesExist)
            {
                yes_Y -= 50;
                dc.DrawImage(yes, new Rect(yes_X, yes_Y, 50, 50));
            }
            if (noExist)
            {
                no_Y -= 50;
                dc.DrawImage(no, new Rect(no_X, no_Y, 50, 50));
            }

            // when yes and no are out of the screen
            // reset their position y and set them to not exist
            if (yes_Y < - 25)
            {
                yes_Y = 900;
                yesExist = false;
            }
            if (no_Y < - 25)
            {
                no_Y = 900;
                noExist = false;
            } 
        }

        // my main contribution
        private Point parentPoint = new Point(200, 50);
        private bool rightDirection = true;

        private void DrawParent(DrawingContext dc)
        {
            // moving right or left
            if (rightDirection)
                parentPoint.X += 10;
            else parentPoint.X -= 10;

            // when reaching the end then move in inverse direction
            if (parentPoint.X > drawingImgWidth - 300 || parentPoint.X < 180)
                rightDirection = !rightDirection;

            // draw the image
            dc.DrawImage(parentCharacter, new Rect(parentPoint.X, parentPoint.Y, 200, 150));
        }

        // my main contribution
        private Random random = new Random();
        private int target;
        private int maxNum = 8;
        private int target_Y = 50;
        private double target_X;
        private bool targetExist = false;
        Vector dir = new Vector(0, 0);

        private void DrawItems(DrawingContext dc, Point playerPoint)
        {
            // if target doesn't exist
            // then randomly spawn a new one that didn't finished by the player
            if (targetExist == false)
            {
                target = random.Next(maxNum);
                while (foodEaten[target] == 1)
                    target = random.Next(maxNum);
                target_X = parentPoint.X;
                targetExist = true;
            }
            else
            {
                // item exist and keep moving in a direction
                target_X += (int)dir.X;
                target_Y += (int)dir.Y;
            }

            // pulling the item near the player, so that the player can hit the item more easily
            if (target_X >= playerPoint.X)
                dir = new Vector(-(target_X - playerPoint.X)*0.01, 5);
            else dir = new Vector((playerPoint.X - target_X) * 0.01, 5);
          
            // draw the corresponding item
            switch (target)
            {
                case 0: dc.DrawImage(egg, new Rect(target_X, target_Y, 75, 75)); break;
                case 1: dc.DrawImage(apple, new Rect(target_X, target_Y, 75, 75)); break;
                case 2: dc.DrawImage(beef, new Rect(target_X, target_Y, 75, 75)); break;
                case 3: dc.DrawImage(broccoli, new Rect(target_X, target_Y, 75, 75)); break;
                case 4: dc.DrawImage(pudding, new Rect(target_X, target_Y, 75, 75)); break;
                case 5: dc.DrawImage(chickenThigh, new Rect(target_X, target_Y, 75, 75)); break;
                case 6: dc.DrawImage(fish, new Rect(target_X, target_Y, 75, 75)); break;
                case 7: dc.DrawImage(mushroom, new Rect(target_X, target_Y, 75, 75)); break;
                default: break;
            }
        }
        // my main contribution
        private int score = 0;
        private int chance = 3;
        private int targetId = 0;
        private void HitTest()
        {
            // when yes hit the item
            if (yesExist && targetExist)
            {
                if ((target_X > yes_X - 50) && (target_X < yes_X + 50) 
                    && (target_Y > yes_Y - 50) && (target_Y < yes_Y + 50))
                {
                    // when the target is the required target
                    if (targetId == target)
                    {
                        foodEaten[targetId] = 1;
                        while (foodEaten[targetId] == 1)
                        {
                            targetId = random.Next(maxNum);
                        }
                        eat.Play();
                        score += 50;
                    }
                    else 
                    {
                        vomit.Play();
                        score -= 50;
                        --chance;
                    }
                    // reset
                    yesExist = false;
                    yes_Y = 900;
                    targetExist = false;
                    target_Y = 100;
                }
            }
            // when no hit the item
            if (noExist && targetExist)
            {
                if ((target_X < no_X + 50) && (target_X > no_X - 50) 
                    && (target_Y > no_Y - 50) && (target_Y < no_Y + 50))
                {
                    // when the target is not the target
                    if (targetId != target)
                    {
                        throwFood.Play();
                        score += 50;
                    }  
                    else 
                    {
                        throwFood.Play();
                        score -= 50;
                        --chance;
                    }
                    // reset
                    noExist = false;
                    no_Y = 900;
                    targetExist = false;
                    target_Y = 100;
                }
            }
            // when the target is out of the screen range
            if (targetExist && (target_Y > drawingImgHeight))
            {
                score -= 50;
                targetExist = false;
                target_Y = 100;
                throwFood.Play();
                --chance;
            }
        }

    }
}
