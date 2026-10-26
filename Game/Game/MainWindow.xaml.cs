using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Game.Config;
using Game.Models;
using Game.Services;

namespace Game
{
    public partial class MainWindow : Window
    {
        private readonly LevelMap _currentMap = LevelMap.GetSchoolLevel();
        private readonly CharacterSpriteConfig _playerSprites = CharacterSpriteConfig.LoadBlondeMan();
        private readonly CollisionSystem _collisionSystem = new();

        private TileMapRenderer _mapRenderer = null!;
        private QuizManager _quizManager = null!;

        private double _playerX;
        private double _playerY;
        private Direction _currentDirection = Direction.Front;

        private bool _isMoving;
        private bool _moveUp, _moveDown, _moveLeft, _moveRight;
        private bool _isGameStarted = false; // Toegevoegd om te controleren of we in het menu zijn

        private int _animationFrame;
        private DateTime _lastAnimTime = DateTime.Now;
        private readonly Image _playerImage = new();

        public MainWindow()
        {
            InitializeComponent();

            _mapRenderer = new TileMapRenderer(GameCanvas);
            _quizManager = new QuizManager(QuizOverlay, QuizQuestionText, QuizAnswerInput, QuizFeedbackText);

            _playerX = 7 * GameConfig.TileSize;
            _playerY = 6 * GameConfig.TileSize;

            InitializePlayer();
            _mapRenderer.Render(_currentMap, _playerImage);

            CompositionTarget.Rendering += OnRenderFrame;
        }

        // ==========================================
        // START MENU FUNCTIES
        // ==========================================

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // Verberg het start menu
            StartMenuOverlay.Visibility = Visibility.Collapsed;

            // Laat de game canvas (in de ScrollViewer) zien
            GameScrollViewer.Visibility = Visibility.Visible;

            // Zorg dat de canvas direct focus krijgt zodat KeyDown events werken
            GameCanvas.Focus();

            _isGameStarted = true;
        }

        private void Afsluiten_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        // ==========================================
        // GAME FUNCTIES
        // ==========================================

        private void InitializePlayer()
        {
            _playerImage.Width = GameConfig.TileSize;
            _playerImage.Height = GameConfig.TileSize;
            UpdatePlayerSprite();

            Canvas.SetLeft(_playerImage, _playerX);
            Canvas.SetTop(_playerImage, _playerY);
            Canvas.SetZIndex(_playerImage, 10);
        }

        private void OnRenderFrame(object? sender, EventArgs e)
        {
            // Stop rendering als het spel nog niet is gestart of als er een quiz bezig is
            if (!_isGameStarted || _quizManager.IsQuizActive) return;

            UpdateMovement();
            UpdateAnimation();

            // Wacht tot de juiste map grootte is gegeven.
            if (GameScrollViewer.ActualWidth <= 0 ||
                GameScrollViewer.ActualHeight <= 0)
                return;

            // Bepaal scroll offset nodig om de speler in het midden te zetten.
            double scrollX = _playerX + (GameConfig.TileSize / 2.0) - (GameScrollViewer.ViewportWidth / 2.0);
            double scrollY = _playerY + (GameConfig.TileSize / 2.0) - (GameScrollViewer.ViewportHeight / 2.0);

            // zorg dat de camera niet Out Of Bounds gaat.
            double maxScrollX = Math.Max(0, GameScrollViewer.ExtentWidth - GameScrollViewer.ViewportWidth);
            double maxScrollY = Math.Max(0, GameScrollViewer.ExtentHeight - GameScrollViewer.ViewportHeight);

            // En beweeg pas dan de camera.
            GameScrollViewer.ScrollToHorizontalOffset(scrollX);
            GameScrollViewer.ScrollToVerticalOffset(scrollY);
        }

        private void UpdateMovement()
        {
            double nextX = _playerX;
            double nextY = _playerY;

            _isMoving = _moveUp || _moveDown || _moveLeft || _moveRight;

            if (_moveUp) { nextY -= GameConfig.DefaultPlayerSpeed; _currentDirection = Direction.Back; }
            if (_moveDown) { nextY += GameConfig.DefaultPlayerSpeed; _currentDirection = Direction.Front; }
            if (_moveLeft) { nextX -= GameConfig.DefaultPlayerSpeed; _currentDirection = Direction.Left; }
            if (_moveRight) { nextX += GameConfig.DefaultPlayerSpeed; _currentDirection = Direction.Right; }

            bool collided = _collisionSystem.IsColliding(nextX, nextY, _currentMap, doorTileId =>
            {
                _moveUp = _moveDown = _moveLeft = _moveRight = false;
                _quizManager.TriggerQuiz(doorTileId, _currentMap);
            });

            if (!collided)
            {
                _playerX = nextX;
                _playerY = nextY;
            }

            Canvas.SetLeft(_playerImage, _playerX);
            Canvas.SetTop(_playerImage, _playerY);
        }

        private void SubmitAnswer_Click(object sender, RoutedEventArgs e)
        {
            _quizManager.SubmitAnswer(_currentMap, () => _mapRenderer.Render(_currentMap, _playerImage));
            GameCanvas.Focus(); // Herstel focus na sluiten quiz
        }

        private void QuizAnswerInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _quizManager.SubmitAnswer(_currentMap, () => _mapRenderer.Render(_currentMap, _playerImage));
                GameCanvas.Focus(); // Herstel focus na sluiten quiz
            }
        }

        private void CancelQuiz_Click(object sender, RoutedEventArgs e)
        {
            _quizManager.CloseQuiz();
            GameCanvas.Focus(); // Herstel focus na sluiten quiz
        }

        private void UpdateAnimation()
        {
            if ((DateTime.Now - _lastAnimTime).TotalMilliseconds >= GameConfig.AnimationFrameRateMs)
            {
                _animationFrame++;
                _lastAnimTime = DateTime.Now;
                UpdatePlayerSprite();
            }
        }

        private void UpdatePlayerSprite()
        {
            var frameList = _isMoving
                ? _playerSprites.WalkFrames[_currentDirection]
                : _playerSprites.IdleFrames[_currentDirection];

            if (frameList.Count == 0) return;

            string relativePath = frameList[_animationFrame % frameList.Count];
            _playerImage.Source = AssetCache.GetImage(relativePath);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (!_isGameStarted || _quizManager.IsQuizActive) return;
            SetMovementState(e.Key, true);
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            SetMovementState(e.Key, false);
        }

        private void SetMovementState(Key key, bool state)
        {
            if (key == Key.W || key == Key.Up) _moveUp = state;
            if (key == Key.S || key == Key.Down) _moveDown = state;
            if (key == Key.A || key == Key.Left) _moveLeft = state;
            if (key == Key.D || key == Key.Right) _moveRight = state;
        }
    }
}