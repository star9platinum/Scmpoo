using System;
using System.Drawing;

namespace Scmpoo.Modern.Animation;

// Translated from the reconstructed original C state machine. State numbers
// and frame tables are retained for traceability and animation parity.
public sealed partial class SheepActor
{
    private int _direction = 1;

    private int _companionDirection = 1;

    private int _gravityEnabled = 0;

    private int _collideWithWindows = 0;

    private int _x = 0;

    private int _y = 0;

    private int _sprite = 0;

    private int _verticalSpeed = 0;

    private int _horizontalSpeed = 0;

    private int _previousY = 0;

    private int _companionX = 0;

    private int _companionY = 0;

    private int _companionSprite = 0;

    private int _frameCounter = 0;

    private int _durationCounter = 0;

    private int _actionVariant = 0;

    private int _movementLocked = 0;

    private int _periodCounter = 0;

    private int _targetX = 0;

    private int _targetY = 0;

    private int _hasBounced = 0;

    private int _fallVariant = 0;

    private int _collisionHeight = 0;

    private int _collisionSpinCounter = 0;

    private int _collisionFrame = 0;

    private int _state = 0;

    private int _mainAboveCompanion = 0;

    private int _preventSpecial = 0;

    private int _fadeStep = 0;

    private int _pendingSleepState = 0;

    private int _retainCompanion = 0;

    private int _companionBeamHeight = 0;

    private int _beamHeight = 0;

    private int _sleeping = 0;

    private static readonly int[] NormalActions = { /* Normal action table (option "Gravity always on" disabled). */
        11, 11, 7, 7,
        11, 11, 7, 7,
        11, 11, 7, 7,
        11, 11, 7, 7,
        11, 11, 7, 7,
        11, 11, 7, 7,
        11, 11, 7, 7,
        11, 11, 7, 7,
        11, 11, 7, 7,
        11, 11, 7, 7,
        11, 7, 17, 20,
        11, 7, 17, 20,
        11, 7, 17, 20,
        11, 7, 17, 20,
        11, 7, 17, 20,
        11, 7, 17, 20,
        11, 7, 17, 20,
        13, 58, 15, 45,
        35, 53, 43, 47,
        45, 47, 49, 51
    };

    private static readonly int[] GravityActions = { /* Normal action table (option "Gravity always on" enabled). */
        11, 11, 7, 7,
        11, 11, 7, 7,
        11, 11, 7, 7,
        11, 11, 7, 7,
        11, 11, 7, 7,
        11, 11, 7, 7,
        11, 11, 7, 7,
        11, 11, 7, 7,
        11, 11, 7, 7,
        11, 11, 7, 7,
        11, 7, 17, 20,
        11, 7, 17, 20,
        11, 7, 17, 20,
        11, 7, 17, 20,
        11, 7, 17, 20,
        11, 7, 17, 20,
        11, 7, 17, 20,
        13, 58, 15, 65,
        35, 53, 43, 75,
        45, 47, 49, 51
    };

    private static readonly int[] SpecialActions = { /* Special action table. */
        116, 121, 126, 147,
        128, 135, 142, 147
    };

    private static readonly int[,] BlinkFrames = { /* Blink animations. */
        {7, 8, 7, 6, 7, 8, 7, 6},
        {32, 33, 32, 31, 32, 33, 32, 31},
        {74, 75, 74, 73, 74, 75, 74, 73},
        {79, 80, 79, 78, 79, 80, 79, 78},
        {82, 83, 82, 81, 82, 83, 82, 81},
        {35, 36, 35, 34, 35, 36, 35, 34}
    };

    private static readonly int[,] HangFrames = { /* Hang on window top edge animations. */
        {42, 43, 42, 44},
        {46, 47, 46, 47}
    };

    private static readonly int[] CollisionFrames = { /* Collision animation with obsolete height offset. */
        62, 63, 63, 64, 64, 65, 65, 66, 66, 66,
        0, 10, 17, 21, 22, 21, 17, 10, 0, 0
    };

    private static readonly int[] YawnFrames = { /* Yawn animation. */
        37, 38, 39, 39, 39, 38, 37, 3, 37, 3, 0
    };

    private static readonly int[] BaaFrames = { /* Baa animation. */
        71, 72, 71, 72, 71, 72, 3, 0
    };

    private static readonly int[] SneezeFrames = { /* Sneeze animation. */
        107, 108, 109, 109, 3, 3, 3, 110, 111, 110, 111, 3, 0
    };

    private static readonly int[] AmazedFrames = { /* Amazed animation. */
        50, 51, 50, 51, 3, 0
    };

    private static readonly int[] EatFrames = { /* Eat animation. */
        58, 150, 60, 61, 60, 61, 60, 61, 58, 151, 60, 61, 60, 61, 60, 61, 2, 58, 152, 60, 61, 60, 61, 60, 61, 58, 153, 60, 61, 60, 61, 60, 61, 3, 0
    };

    private static readonly int[] BurnFrames = { /* Burn animation. */
        134, 134, 134, 134, 134, 134, 134, 134, 135, 136, 137, 138, 137, 138, 137, 138, 137, 138, 137, 138, 139, 140, 141, 142, 143, 144, 145, 144, 145, 144, 145, 144, 145, 0
    };

    private static readonly int[] RolloverFrames = { /* Roll over animation (not used). */
        3, 93, 99, 100, 99, 100, 99, 100, 99, 100, 95, 3, 0
    };

    private static readonly int[] GetUpLeftFrames = { /* Get up animation (left). */
        48, 48, 48, 49, 13, 12, 3, 0
    };

    private static readonly int[] GetUpRightFrames = { /* Get up animation (right). */
        48, 48, 48, 49, 13, 14, 3, 0
    };

    private static readonly int[] MerryFrames = { /* Merry 2 animation. */
        130, 130, 130, 130, 130, 129, 129, 128, 128, 127, 127, 127, 6, 6, 6, 6, 7, 8, 7, 6, 7, 8, 7, 6, 6, 6, 6, 0
    };

    private static readonly int[] SplashFrames = { /* Burn bathtub splash animation. */
        147, 148, 147, 146, 0
    };

    private static readonly int[] BathExitFrames = { /* Burn get out of bathtub animation. */
        169, 169, 169, 169, 169, 169, 169, 169, 170, 171, 170, 169, 170, 171, 170, 169, 169, 169, 169, 81, 81, 81, 81, 81, 81, 81, 81, 85, 85, 85, 85, 85, 85, 85, 85, 34, 34, 34, 34, 35, 36, 35, 34, 35, 36, 35, 34, 34, 34, 10, 10, 9, 9, 3, 0
    };

    private static readonly int[] BlushFrames = { /* Blush animation. */
        3, 127, 128, 129, 130, 130, 130, 129, 128, 127, 127, 0
    };

    private static readonly int[] RollFrames = { /* Roll animation. */
        119, 120, 121, 122, 123, 124, 125, 126, 0
    };

    private static readonly int[] SpinFrames = { /* Spin animation. 0-3: face, 4-7: back */
        3, 9, 10, 11, 2, 14, 13, 12
    };

    private void AdvanceState()
    {
        IntPtr hitWindow = IntPtr.Zero;
        int var_2;
        int var_4;
        IntPtr var_6 = default;
        IntPtr var_8 = default;
        EngineRect var_10 = default;
        Point var_14 = default;
        EngineRect spawn_area = default;

        UpdateChime();
    DispatchState:
        switch (_state) {
        case 0:
            _gravityEnabled = 0;

            _x = SceneLeft - 80;
            _y = SceneTop - 80;
            _state = 1;
            goto case 1;
        case 1:
            _collisionHeight = 0;
            if (GravityOption != 0) {
                _state = 2;
                goto DispatchState;
            }
            _beamHeight = 0;
            HideCompanion();
            if (_pendingSleepState != 0) {
                _state = _pendingSleepState;
                _pendingSleepState = 0;
                break;
            }
            if (NextRandom() % 20 == 5 && _gravityEnabled == 0) {
                _state = 85;
                break;
            }
            if (ShouldTriggerSpecial(40) && _gravityEnabled == 0 && _preventSpecial == 0) {
                _state = 4;
                break;
            }
            _state = NormalActions[NextRandom() % 80];
            if (!SpriteOnMonitor(_x, _y)) {
                spawn_area = RandomMonitorRect();
                if ((NextRandom() & 1) == 0) {
                    _direction = 1;
                    _x = spawn_area.Right;
                } else {
                    _direction = -1;
                    _x = spawn_area.Left - 40;
                }
                _y = spawn_area.Top + NextRandom() % Math.Max(1, spawn_area.Bottom - spawn_area.Top - 64);
                _state = 11;
            }
            break;
        case 2:
            _gravityEnabled = 1;
            _beamHeight = 0;
            HideCompanion();
            if (_pendingSleepState != 0) {
                _state = _pendingSleepState;
                _pendingSleepState = 0;
                break;
            }
            _state = GravityActions[NextRandom() % 80];
            if (!SpriteOnMonitor(_x, _y)) {
                if (ShouldTriggerSpecial(10) && _preventSpecial == 0) {
                    _state = 6;
                    break;
                }
                _landingWindow = GetForegroundWindow();
                if (_landingWindow == MainWindowHandle || _landingWindow == CompanionWindowHandle || _landingWindow == IntPtr.Zero || IsSheepWindow(_landingWindow)) {
                    _state = 3;
                    goto DispatchState;
                }
                GetSupportRect(_landingWindow, out _landingRect);
                if (_landingRect.Top < SceneTop + 10) {
                    _state = 3;
                    goto DispatchState;
                }
                _x = RandomWindowX(_landingRect);
                GetMonitorRect(_x + 20, _landingRect.Top, out spawn_area);
                _y = spawn_area.Top - 40;
                _hasBounced = 0;
                _verticalSpeed = 0;
                _horizontalSpeed = 0;
                _fallVariant = NextRandom() % 2;
                _state = 92;
                if (NextRandom() % 3 == 0) {
                    _state = 3;
                    goto DispatchState;
                }
            }
            break;
        case 3:
            _gravityEnabled = 1;
            spawn_area = RandomMonitorRect();
            _x = spawn_area.Left + NextRandom() % Math.Max(1, spawn_area.Right - spawn_area.Left - 40);
            _y = spawn_area.Top - NextRandom() % 20 - 40;
            _hasBounced = 0;
            _verticalSpeed = 0;
            _horizontalSpeed = 0;
            _fallVariant = NextRandom() % 2;
            if (NextRandom() % 3 == 0) {
                RaiseWindow(MainWindowHandle);
            }
            _state = 97;
            break;
        case 153:
            break;
        case 154:
            break;
        case 4:
            if (SceneCenterX - 20 > _x) {
                _direction = 1;
            } else {
                _direction = -1;
            }
            _sprite = 4;
            PresentMain(_x, _y, _sprite);
            _state = 5;
            break;
        case 5:
            if (_periodCounter++ < 1) {
                break;
            }
            _periodCounter = 0;
            _x -= _direction * 16;
            _sprite = _sprite == 4 ? 5 : 4;
            PresentMain(_x, _y, _sprite);
            if (_x < SceneLeft - 40 || _x > SceneRight) {
                _state = 6;
            }
            break;
        case 6:
            _state = SpecialActions[NextRandom() % 8];
            break;
        case 7:
            _collideWithWindows = 0;
            if ((NextRandom() & 1) == 0) {
                _collideWithWindows = 1;
            }
            if (_collideWithWindows != 0) {
                RefreshWindowSnapshot();
            }
            _sprite = 4;
            PresentMain(_x, _y, _sprite);
            _frameCounter = NextRandom() % 10 + 10;
            _state = 8;
            break;
        case 8:
            if (_periodCounter++ < 1) {
                break;
            }
            _periodCounter = 0;
            if (_collideWithWindows != 0) {
                if (_direction > 0) {
                    var_2 = FindSideEdge(out var_6, _y, _y + 40, -(_direction * 16 - _x), _x);
                } else {
                    var_2 = FindSideEdge(out var_6, _y, _y + 40, -(_direction * 16 - _x) + 40, _x + 40);
                }
                if (var_2 != NoCollision) {
                    if (_direction > 0) {
                        _x = var_2;
                    } else {
                        _x = var_2 - 40;
                    }
                    PresentMain(_x, _y, _sprite);
                    _state = 30;
                    break;
                }
            }
            if (_movementLocked == 0) {
                _x -= _direction * 16;
            }
            _sprite = _sprite == 4 ? 5 : 4;
            PresentMain(_x, _y, _sprite);
            if (NextRandom() % 50 == 0 && _gravityEnabled != 0) {
                _state = 9;
            }
            CheckRunningBoundary(true);
            FinishTimedAction();
            CheckSupport(2);
            CheckSheepCollision(-(_direction * 16 - _x), _direction * 16 + _x, 2);
            break;
        case 9:
            _verticalSpeed = -11;
            _horizontalSpeed = -(_direction * 8);
            _previousY = _y;
            _state = 10;
            goto case 10;
        case 10:
            _x += _horizontalSpeed;
            _y += _verticalSpeed;
            _verticalSpeed += 2;
            if (_verticalSpeed >= -1 && _verticalSpeed <= 1) {
                _sprite = 23;
            } else if (_verticalSpeed < -1) {
                _sprite = 30;
            } else {
                _sprite = 24;
            }
            if (_previousY <= _y) {
                _y = _previousY;
                _state = 7;
            }
            PresentMain(_x, _y, _sprite);
            CheckRunningBoundary(false);
            CheckSheepCollision(_horizontalSpeed + _x, _x - _horizontalSpeed, 2);
            if (_state == 30 && _previousY != _y) {
                _collisionHeight = _y - _previousY;
            }
            break;
        case 11:
            _collideWithWindows = 0;
            if ((_gravityEnabled & ((NextRandom() & 1) == 0 ? 1 : 0)) != 0) {
                _collideWithWindows = 1;
            }
            if (_collideWithWindows != 0) {
                RefreshWindowSnapshot();
            }
            _frameCounter = NextRandom() % 10 + 10;
            _sprite = 2;
            PresentMain(_x, _y, _sprite);
            _state = 12;
            break;
        case 12:
            if (_periodCounter++ < 1) {
                break;
            }
            _periodCounter = 0;
            if (_collideWithWindows != 0) {
                if (_direction > 0) {
                    var_2 = FindSideEdge(out var_6, _y, _y + 40, -(_direction * 6 - _x), _x);
                } else {
                    var_2 = FindSideEdge(out var_6, _y, _y + 40, -(_direction * 6 - _x) + 40, _x + 40);
                }
                if (var_2 != NoCollision) {
                    if (_direction > 0) {
                        _x = var_2;
                    } else {
                        _x = var_2 - 40;
                    }
                    _landingWindow = var_6;
                    GetSupportRect(_landingWindow, out _landingRect);
                    _targetY = _landingRect.Top - 12;
                    _gravityEnabled = 1;
                    _targetX = _x;
                    _sprite = 30;
                    PlaceWindowBehind(MainWindowHandle, _landingWindow);
                    _state = 89;
                    break;
                }
            }
            if (_movementLocked == 0) {
                _x -= _direction * 6;
            }
            _sprite = _sprite == 2 ? 3 : 2;
            PresentMain(_x, _y, _sprite);
            TurnAtBoundary();
            FinishTimedAction();
            CheckSupport(1);
            CheckSheepCollision(-(_direction * 6 - _x), _direction * 6 + _x, 1);
            break;
        case 13:
            _actionVariant = NextRandom() % 2;
            _frameCounter = NextRandom() % 4 + 4;
            if (_actionVariant != 0) {
                _sprite = 88;
            } else {
                _sprite = 86;
            }
            PresentMain(_x, _y, _sprite);
            _state = 14;
            break;
        case 14:
            if (_periodCounter++ < 3) {
                break;
            }
            _periodCounter = 0;
            if (_movementLocked == 0) {
                _x -= _direction * 6;
            }
            if (_actionVariant != 0) {
                _sprite = _sprite == 88 ? 89 : 88;
            } else {
                _sprite = _sprite == 86 ? 87 : 86;
            }
            PresentMain(_x, _y, _sprite);
            TurnAtBoundary();
            FinishTimedAction();
            CheckSupport(1);
            break;
        case 15:
            _actionVariant = NextRandom() % 2;
            _frameCounter = NextRandom() % 3 + 3;
            if (_actionVariant != 0) {
                _sprite = 54;
            } else {
                _sprite = 52;
            }
            PresentMain(_x, _y, _sprite);
            _state = 16;
            break;
        case 16:
            if (_periodCounter++ < 3) {
                break;
            }
            _periodCounter = 0;
            if (_actionVariant != 0) {
                _sprite = _sprite == 54 ? 55 : 54;
            } else {
                _sprite = _sprite == 52 ? 53 : 52;
            }
            PresentMain(_x, _y, _sprite);
            TurnAtBoundary();
            FinishTimedAction();
            CheckSupport(0);
            break;
        case 17:
            _sprite = 6;
            PresentMain(_x, _y, _sprite);
            _state = 18;
            break;
        case 18:
            if (_periodCounter++ < 1) {
                break;
            }
            _periodCounter = 0;
            _sprite += 1;
            PresentMain(_x, _y, _sprite);
            if (_sprite == 8) {
                _sprite = 0;
                _state = 19;
                _frameCounter = NextRandom() % 8 + 8;
            }
            CheckSupport(0);
            break;
        case 19:
            if (_periodCounter++ < 4) {
                break;
            }
            _periodCounter = 0;
            _sprite = _sprite == 0 ? 1 : 0;
            PresentMain(_x, _y, _sprite);
            FinishTimedAction();
            CheckSupport(0);
            break;
        case 20:
            _actionVariant = NextRandom() % 3;
            if (_actionVariant == 0) {
                _sprite = 6;
            } else if (_actionVariant == 1) {
                _sprite = 31;
            } else {
                _sprite = 73;
            }
            PresentMain(_x, _y, _sprite);
            _state = 21;
            _periodCounter = NextRandom() % 15 + NextRandom() % 15;
            CheckSupport(0);
            break;
        case 21:
            CheckSupport(0);
            if (_periodCounter-- > 0) {
                break;
            }
            _state = 22;
            _frameCounter = 0;
            break;
        case 22:
            _sprite = BlinkFrames[_actionVariant, _frameCounter];
            PresentMain(_x, _y, _sprite);
            _frameCounter += 1;
            if (_frameCounter > 7) {
                _state = 23;
                _periodCounter = NextRandom() % 15 + NextRandom() % 15;
            }
            CheckSupport(0);
            break;
        case 23:
            CheckSupport(0);
            if (_periodCounter-- > 0) {
                break;
            }
            _state = 1;
            break;
        case 24:
            if (_periodCounter++ < 1) {
                break;
            }
            _periodCounter = 0;
            _sprite = 3;
            PresentMain(_x, _y, _sprite);
            if ((NextRandom() & 1) != 0) {
                _actionVariant = 0;
            } else {
                _actionVariant = 1;
            }
            _state = 25;
            _frameCounter = 0;
            break;
        case 25:
            if (_periodCounter++ < 1) {
                break;
            }
            _periodCounter = 0;
            if (_actionVariant != 0) {
                if (_direction > 0) {
                    _sprite = _frameCounter + 9;
                } else {
                    _sprite = 11 - _frameCounter;
                }
            } else {
                if (_direction > 0) {
                    _sprite = _frameCounter + 12;
                } else {
                    _sprite = 14 - _frameCounter;
                }
            }
            PresentMain(_x, _y, _sprite);
            _frameCounter += 1;
            if (_frameCounter > 2) {
                _direction = -_direction;
                _state = 26;
            }
            CheckSupport(0);
            break;
        case 26:
            if (_periodCounter++ < 1) {
                break;
            }
            _periodCounter = 0;
            _sprite = 3;
            PresentMain(_x, _y, _sprite);
            _state = 1;
            CheckSupport(0);
            break;
        case 27:
            _verticalSpeed = -10;
            _horizontalSpeed = _direction * 8;
            _previousY = _y;
            _collisionFrame = 0;
            _state = 28;
            goto case 28;
        case 28:
            _x += _horizontalSpeed;
            _y += _verticalSpeed;
            _verticalSpeed += 2;
            _sprite = CollisionFrames[_collisionFrame];
            _collisionFrame += 1;
            PresentMain(_x, _y, _sprite);
            if (_sprite == 64) {
                _fallVariant = 3;
                _state = 99;
                break;
            }
            break;
        case 29:
            _periodCounter = 0;
            _frameCounter = 0;
            _actionVariant = 0;
            if ((NextRandom() & 7) == 0) {
                _actionVariant = 1;
            }
            if (NextRandom() % 5 == 0) {
                _actionVariant = 2;
            }
            _state = 32;
            if (_actionVariant != 0) {
                _state = 34;
            }
            goto DispatchState;
        case 30:
            if (_gravityEnabled != 0) {
                _state = 27;
                goto DispatchState;
            } else {
                _state = 24;
                goto DispatchState;
            }
        case 31:
            CheckSheepCollision(_direction * 10 + _x, _x, 2);
            if (_state == 30) {
                if (_frameCounter != 0) {
                    _collisionHeight -= CollisionFrames[_frameCounter + 9];
                }
                break;
            }
            _sprite = CollisionFrames[_frameCounter];
            PresentMain(_x, _y - CollisionFrames[_frameCounter + 10], _sprite);
            _frameCounter += 1;
            if (_actionVariant != 0 && _sprite == 66) {
                if (_collisionHeight != 0) {
                    _y -= _collisionHeight;
                    _x += _direction * 10;
                    PresentMain(_x, _y, _sprite);
                }
                _collisionSpinCounter = 3;
                _state = 34;
                break;
            }
            if (_frameCounter > 8) {
                _state = 32;
                break;
            }
            _x += _direction * 10;
            break;
        case 32:
            CheckSupport(0);
            if (_periodCounter++ < 8) {
                break;
            }
            _periodCounter = 0;
            _direction = -_direction;
            _sprite = 93;
            PresentMain(_x, _y, _sprite);
            _state = 33;
            break;
        case 33:
            CheckSupport(0);
            if (_periodCounter++ < 15) {
                break;
            }
            _periodCounter = 0;
            _state = 1;
            break;
        case 34:
            _x += _direction * 8;
            if (_sprite == 70) {
                _sprite = 63;
            } else {
                _sprite += 1;
            }
            PresentMain(_x, _y, _sprite);
            if (_actionVariant == 2 && _sprite == 70) {
                _state = 69;
                break;
            }
            if (_x > SceneRight || _x < SceneLeft - 40) {
                _state = 1;
            }
            CheckSheepCollision(_direction * 8 + _x, -(_direction * 8 - _x), 2);
            if (_state == 30) {
                if (_collisionSpinCounter-- > 0) {
                    _direction = -_direction;
                    _state = 34;
                } else {
                    _state = 34;
                }
            }
            CheckSupport(2);
            break;
        case 35:
            if (_periodCounter++ < 1) {
                break;
            }
            _periodCounter = 0;
            _sprite = 3;
            PresentMain(_x, _y, _sprite);
            _state = 37;
            _frameCounter = 0;
            break;
        case 36:
            break;
        case 37:
            if (_periodCounter++ < 1) {
                break;
            }
            _periodCounter = 0;
            if (_direction > 0) {
                _sprite = _frameCounter + 12;
            } else {
                _sprite = 14 - _frameCounter;
            }
            PresentMain(_x, _y, _sprite);
            _frameCounter += 1;
            if (_frameCounter > 1) {
                _sprite = 103;
                _state = 38;
            }
            CheckSupport(0);
            break;
        case 38:
            if (_periodCounter++ < 1) {
                break;
            }
            _periodCounter = 0;
            PresentMain(_x, _y, _sprite);
            _sprite += 1;
            if (_sprite > 104) {
                _frameCounter = 0;
                _state = 39;
                break;
            }
            CheckSupport(0);
            break;
        case 39:
            if (_frameCounter == 0) {
                if (_periodCounter++ < 10) {
                    break;
                }
                _periodCounter = 0;
            } else {
                if (_periodCounter++ < 1) {
                    break;
                }
                _periodCounter = 0;
            }
            if (_frameCounter <= 8 || _frameCounter >= 12 && _frameCounter <= 12) {
                _sprite = _sprite == 105 ? 106 : 105;
            } else {
                _sprite = 104;
            }
            PresentMain(_x, _y, _sprite);
            if (_frameCounter++ > 15) {
                _state = 40;
                _sprite = 104;
            }
            CheckSupport(0);
            break;
        case 40:
            if (_periodCounter++ < 1) {
                break;
            }
            _periodCounter = 0;
            PresentMain(_x, _y, _sprite);
            if (--_sprite < 103) {
                _frameCounter = 0;
                _state = 41;
                break;
            }
            CheckSupport(0);
            break;
        case 41:
            if (_periodCounter++ < 1) {
                break;
            }
            _periodCounter = 0;
            if (_direction > 0) {
                _sprite = 13 - _frameCounter;
            } else {
                _sprite = _frameCounter + 13;
            }
            PresentMain(_x, _y, _sprite);
            _frameCounter += 1;
            if (_frameCounter > 1) {
                _state = 42;
            }
            CheckSupport(0);
            break;
        case 42:
            CheckSupport(0);
            if (_periodCounter++ < 1) {
                break;
            }
            _periodCounter = 0;
            _sprite = 3;
            PresentMain(_x, _y, _sprite);
            _state = 1;
            _frameCounter = 0;
            break;
        case 43:
            PlaySound(109, 0, 0);
            _frameCounter = 0;
            _state = 44;
            goto case 44;
        case 44:
            if (_periodCounter++ < 1) {
                break;
            }
            _periodCounter = 0;
            _sprite = YawnFrames[_frameCounter];
            _frameCounter += 1;
            if (_sprite == 0) {
                _state = 1;
                break;
            }
            PresentMain(_x, _y, _sprite);
            CheckSupport(0);
            break;
        case 45:
            PlaySound(108, 0, 0);
            _frameCounter = 0;
            _state = 46;
            goto case 46;
        case 46:
            if (_periodCounter++ < 0) {
                break;
            }
            _periodCounter = 0;
            _sprite = BaaFrames[_frameCounter];
            _frameCounter += 1;
            if (_sprite == 0) {
                _state = 1;
                break;
            }
            PresentMain(_x, _y, _sprite);
            CheckSupport(0);
            break;
        case 47:
            _frameCounter = 0;
            _state = 48;
            goto case 48;
        case 48:
            if (_periodCounter++ < 0) {
                break;
            }
            _periodCounter = 0;
            if (_frameCounter == 2) {
                PlaySound(110, 0, 0);
            }
            _sprite = SneezeFrames[_frameCounter];
            _frameCounter += 1;
            if (_sprite == 0) {
                _state = 1;
                break;
            }
            PresentMain(_x, _y, _sprite);
            CheckSupport(0);
            break;
        case 49:
            _frameCounter = 0;
            _state = 50;
            goto case 50;
        case 50:
            if (_periodCounter++ < 1) {
                break;
            }
            _periodCounter = 0;
            _sprite = AmazedFrames[_frameCounter];
            _frameCounter += 1;
            if (_sprite == 0) {
                _state = 1;
                break;
            }
            PresentMain(_x, _y, _sprite);
            CheckSupport(0);
            break;
        case 51:
            _frameCounter = 0;
            _state = 52;
            goto case 52;
        case 52:
            if (_periodCounter++ < 0) {
                break;
            }
            _periodCounter = 0;
            _sprite = _sprite == 56 ? 57 : 56;
            if (_frameCounter++ > 30) {
                _sprite = 3;
                _state = 1;
            }
            PresentMain(_x, _y, _sprite);
            CheckSupport(0);
            break;
        case 53:
            _frameCounter = 0;
            _state = 54;
            ShowCompanion();
            _companionDirection = _direction;
            _companionY = _y;
            _companionSprite = 149;
            if (_direction > 0) {
                _companionX = _x - 40;
            } else {
                _companionX = _x + 40;
            }
            PresentCompanion(_companionX, _companionY, _companionSprite);
            _mainAboveCompanion = 1;
            PlaceWindowBehind(CompanionWindowHandle, MainWindowHandle);
            break;
        case 54:
            if (_periodCounter++ < 2) {
                break;
            }
            _periodCounter = 0;
            _sprite = EatFrames[_frameCounter];
            _frameCounter += 1;
            if (_sprite == 2) {
                _x -= _direction * 8;
                PresentMain(_x, _y, _sprite);
                break;
            }
            if (_sprite >= 149 && _sprite <= 153) {
                _companionSprite = _sprite;
                if (_companionSprite == 153) {
                    _companionSprite = 173;
                }
                PresentCompanion(_companionX, _companionY, _companionSprite);
                _sprite = EatFrames[_frameCounter];
                _frameCounter += 1;
            }
            if (_sprite == 0) {
                HideCompanion();
                _state = 1;
                break;
            }
            PresentMain(_x, _y, _sprite);
            break;
        case 55:
            break;
        case 56:
            _frameCounter = 0;
            _state = 57;
            goto case 57;
        case 57:
            if (_periodCounter++ < 2) {
                break;
            }
            _periodCounter = 0;
            _sprite = EatFrames[_frameCounter];
            _frameCounter += 1;
            if (_sprite >= 149 && _sprite <= 153) {
                _sprite = EatFrames[_frameCounter];
                _frameCounter += 1;
            }
            if (_frameCounter >= 16) {
                _state = 42;
                break;
            }
            PresentMain(_x, _y, _sprite);
            CheckSupport(0);
            break;
        case 58:
            if (_periodCounter++ < 1) {
                break;
            }
            _periodCounter = 0;
            _sprite = 3;
            PresentMain(_x, _y, _sprite);
            _state = 59;
            _frameCounter = 0;
            break;
        case 59:
            if (_periodCounter++ < 1) {
                break;
            }
            _periodCounter = 0;
            if (_direction > 0) {
                _sprite = _frameCounter + 9;
            } else {
                _sprite = 11 - _frameCounter;
            }
            _frameCounter += 1;
            if (_frameCounter > 2) {
                _sprite = 34;
                _periodCounter = -10;
                _state = 60;
                _frameCounter = 0;
            }
            PresentMain(_x, _y, _sprite);
            CheckSupport(0);
            break;
        case 60:
            if (_periodCounter++ < 0) {
                break;
            }
            _periodCounter = 0;
            _sprite = BlinkFrames[5, _frameCounter];
            PresentMain(_x, _y, _sprite);
            _frameCounter += 1;
            if (_frameCounter > 7) {
                _frameCounter = 0;
                _state = 61;
                _periodCounter = -5;
            }
            CheckSupport(0);
            break;
        case 61:
            if (_periodCounter++ < 1) {
                break;
            }
            _periodCounter = 0;
            if (_direction > 0) {
                _sprite = 10 - _frameCounter;
            } else {
                _sprite = _frameCounter + 10;
            }
            PresentMain(_x, _y, _sprite);
            _frameCounter += 1;
            if (_frameCounter > 1) {
                _state = 42;
            }
            CheckSupport(0);
            break;
        case 64:
            break;
        case 65:
            _sprite = 3;
            PresentMain(_x, _y, _sprite);
            _frameCounter = 0;
            _state = 66;
            break;
        case 66:
            if (_periodCounter++ < 1) {
                break;
            }
            _periodCounter = 0;
            if (_frameCounter == 0) {
                if (_direction > 0) {
                    _sprite = 9;
                } else {
                    _sprite = 11;
                }
            } else {
                _sprite = 10;
            }
            PresentMain(_x, _y, _sprite);
            if (_frameCounter++ > 0) {
                _state = 67;
                _durationCounter = (NextRandom() % 4 + 4) * 8;
                _frameCounter = 0;
                break;
            }
            CheckSupport(0);
            break;
        case 67:
            if (--_frameCounter < 0) {
                _frameCounter = 79;
            }
            _x -= _direction * 8;
            _sprite = RollFrames[_frameCounter % 8];
            PresentMain(_x, _y, _sprite);
            if (_direction > 0 && _x < SceneLeft) {
                _state = 30;
            }
            if (_direction < 0 && SceneRight - 40 < _x) {
                _state = 30;
            }
            if (--_durationCounter <= 0) {
                _state = 68;
                _frameCounter = 0;
            }
            CheckSupport(2);
            CheckSheepCollision(-(_direction * 8 - _x), _direction * 8 + _x, 2);
            break;
        case 68:
            if (_periodCounter++ < 1) {
                break;
            }
            _periodCounter = 0;
            if (_frameCounter == 1) {
                if (_direction > 0) {
                    _sprite = 9;
                } else {
                    _sprite = 11;
                }
            } else if (_frameCounter == 0) {
                _sprite = 10;
            } else {
                _sprite = 3;
            }
            PresentMain(_x, _y, _sprite);
            if (_frameCounter++ > 1) {
                _state = 1;
                break;
            }
            CheckSupport(0);
            break;
        case 62:
            _state = 63;
            _frameCounter = 0;
            break;
        case 63:
            if (_periodCounter++ < 2) {
                break;
            }
            _periodCounter = 0;
            _sprite = BlushFrames[_frameCounter];
            if (_sprite == 0) {
                _state = 1;
                break;
            }
            PresentMain(_x, _y, _sprite);
            _frameCounter += 1;
            CheckSupport(0);
            break;
        case 75:
            _frameCounter = NextRandom() % 8 + 8;
            _durationCounter = _frameCounter;
            _sprite = 131;
            if (_direction > 0) {
                _sprite = 12;
            } else {
                _sprite = 14;
            }
            PresentMain(_x, _y, _sprite);
            _state = 76;
            break;
        case 76:
            if (_periodCounter++ < 1) {
                break;
            }
            _periodCounter = 0;
            _sprite = 13;
            PresentMain(_x, _y, _sprite);
            _state = 77;
            break;
        case 77:
            if (_periodCounter++ < 2) {
                break;
            }
            _periodCounter = 0;
            _sprite = _sprite == 131 ? 132 : 131;
            _y -= 8;
            PresentMain(_x, _y, _sprite);
            if (_frameCounter-- <= 0) {
                _frameCounter = _durationCounter;
                _state = 78;
            }
            break;
        case 78:
            _sprite = 133;
            _y += 8;
            PresentMain(_x, _y, _sprite);
            if (_frameCounter-- <= 0) {
                _state = 79;
            }
            break;
        case 79:
            if (_periodCounter++ < 10) {
                break;
            }
            _periodCounter = 0;
            _state = 80;
            _frameCounter = 3;
            break;
        case 80:
            if (_periodCounter++ < 1) {
                break;
            }
            _periodCounter = 0;
            if (_direction > 0) {
                _sprite = GetUpLeftFrames[_frameCounter];
                _frameCounter += 1;
            } else {
                _sprite = GetUpRightFrames[_frameCounter];
                _frameCounter += 1;
            }
            if (_sprite == 0) {
                _state = 1;
                break;
            }
            PresentMain(_x, _y, _sprite);
            break;
        case 69:
            _periodCounter = 0;
            _frameCounter = 0;
            _state = 70;
            goto case 70;
        case 70:
            if (_direction > 0) {
                _sprite = SpinFrames[_frameCounter % 8];
            } else {
                _sprite = SpinFrames[(_frameCounter + 4) % 8];
            }
            if (_sprite == 2) {
                _sprite = 3;
                if (_direction > 0) {
                    _direction = -_direction;
                    PresentMain(_x, _y, _sprite);
                    _direction = -_direction;
                } else {
                    PresentMain(_x, _y, _sprite);
                }
            } else if (_sprite == 3) {
                if (_direction < 0) {
                    _direction = -_direction;
                    PresentMain(_x, _y, _sprite);
                    _direction = -_direction;
                } else {
                    PresentMain(_x, _y, _sprite);
                }
            } else {
                PresentMain(_x, _y, _sprite);
            }
            if (_frameCounter++ >= 16) {
                _sprite = 70;
                PresentMain(_x, _y, _sprite);
                _state = 71;
            }
            CheckSupport(0);
            break;
        case 71:
            CheckSupport(0);
            if (_periodCounter++ < 14) {
                break;
            }
            _periodCounter = 0;
            _sprite = 96;
            PresentMain(_x, _y, _sprite);
            _state = 72;
            break;
        case 72:
            CheckSupport(0);
            if (_periodCounter++ < 30) {
                break;
            }
            _periodCounter = 0;
            _sprite = 3;
            PresentMain(_x, _y, _sprite);
            _state = 1;
            break;
        case 73:
            _frameCounter = 0;
            _state = 74;
            goto case 74;
        case 74:
            if (_periodCounter++ < 2) {
                break;
            }
            _periodCounter = 0;
            _sprite = RolloverFrames[_frameCounter];
            _frameCounter += 1;
            if (_sprite == 0) {
                _state = 1;
                break;
            }
            PresentMain(_x, _y, _sprite);
            CheckSupport(0);
            break;
        case 81:
            _sprite = 4;
            PresentMain(_x, _y, _sprite);
            _state = 82;
            break;
        case 82:
            _periodCounter = 0;
            _sprite = _sprite == 4 ? 5 : 4;
            PresentMain(_x, _y, _sprite);
            break;
        case 83:
            break;
        case 84:
            _state = 0;
            break;
        case 85:
            _landingWindow = GetForegroundWindow();
            if (_landingWindow == MainWindowHandle || _landingWindow == CompanionWindowHandle || _landingWindow == IntPtr.Zero || IsSheepWindow(_landingWindow)) {
                _state = 1;
                break;
            }
            GetSupportRect(_landingWindow, out _landingRect);
            if (_landingRect.Top < SceneTop + 10) {
                _state = 1;
                break;
            }
            if (_direction > 0 && _landingRect.Right < _x && _landingRect.Top < _y && _y + 40 < _landingRect.Bottom || _direction < 0 && _x + 40 < _landingRect.Left && _landingRect.Top < _y && _y + 40 < _landingRect.Bottom) {
                _state = 87;
                break;
            }
            _targetX = RandomWindowX(_landingRect);
            _targetY = _landingRect.Top - 40;
            if (SceneCenterX - 20 > _x) {
                _direction = 1;
            } else {
                _direction = -1;
            }
            _sprite = 4;
            PresentMain(_x, _y, _sprite);
            _state = 86;
            break;
        case 86:
            if (_periodCounter++ < 1) {
                break;
            }
            _periodCounter = 0;
            _x -= _direction * 16;
            _sprite = _sprite == 4 ? 5 : 4;
            PresentMain(_x, _y, _sprite);
            if (_x < SceneLeft - 40 || _x > SceneRight) {
                if (!IsSupportValid(_landingWindow)) {
                    _state = 1;
                    break;
                }
                if (NextRandom() % 3 == 0) {
                    _state = 3;
                    goto DispatchState;
                }
                _hasBounced = 0;
                _state = 92;
                _gravityEnabled = 1;
                _x = _targetX;
                GetMonitorRect(_x + 20, _landingRect.Top, out spawn_area);
                _y = spawn_area.Top - 40;
                _verticalSpeed = 0;
                _horizontalSpeed = 0;
                _fallVariant = NextRandom() % 2;
                if (_fallVariant != 0) {
                    _horizontalSpeed = -(_direction * 3);
                }
                PlaceWindowBehind(MainWindowHandle, _landingWindow);
            }
            break;
        case 87:
            PlaceWindowBehind(MainWindowHandle, _landingWindow);
            if (_direction > 0) {
                _targetX = _landingRect.Right;
                _targetY = _landingRect.Top;
            } else {
                _targetX = _landingRect.Left - 40;
                _targetY = _landingRect.Top;
            }
            _state = 88;
            goto case 88;
        case 88:
            if (_periodCounter++ < 1) {
                break;
            }
            _periodCounter = 0;
            _x -= _direction * 16;
            _sprite = _sprite == 4 ? 5 : 4;
            if (_targetX >= _x && _direction > 0 || _targetX <= _x && _direction < 0) {
                if (!IsSupportValid(_landingWindow)) {
                    _state = 1;
                    break;
                }
                GetSupportRect(_landingWindow, out var_10);
                if (var_10.Left == _landingRect.Left && var_10.Right == _landingRect.Right && var_10.Top < _y && _y + 40 < var_10.Bottom) {
                    _targetY = var_10.Top - 12;
                    _gravityEnabled = 1;
                    _x = _targetX;
                    _sprite = 30;
                    _state = 89;
                    break;
                } else {
                    _state = 1;
                    break;
                }
            }
            PresentMain(_x, _y, _sprite);
            break;
        case 89:
            if (_periodCounter++ < 1) {
                break;
            }
            _periodCounter = 0;
            PresentMain(_x, _y, _sprite);
            _y -= 6;
            _sprite = _sprite == 15 ? 16 : 15;
            if (_targetY >= _y) {
                _state = 90;
                break;
            }
            CheckClimbingWindow();
            break;
        case 90:
            if (_periodCounter++ < 2) {
                break;
            }
            _periodCounter = 0;
            _x -= _direction * 8;
            _y = _targetY - 20;
            _sprite = 76;
            PresentMain(_x, _y, _sprite);
            _state = 91;
            break;
        case 91:
            if (_periodCounter++ < 2) {
                break;
            }
            _periodCounter = 0;
            _x += _direction * -24;
            _y -= 8;
            _sprite = 3;
            PresentMain(_x, _y, _sprite);
            _state = 11;
            break;
        case 92:
            _verticalSpeed += 4;
            _previousY = _y;
            _x += _horizontalSpeed;
            _y += _verticalSpeed;
            if ((var_4 = FindSpecificLandingEdge(_landingWindow, 40 + _y, 40 + _previousY, _x, 40 + _x)) != NoCollision) {
                if (var_4 == BelowFloor) {
                    PresentMain(_x, _y, _sprite);
                    _state = 0;
                    break;
                }
                _y = var_4 - 40;
                if (_verticalSpeed < 64 && _hasBounced == 0 || _verticalSpeed < 8) {
                    PlaceWindowBehind(MainWindowHandle, _landingWindow);
                    _hasBounced = 0;
                    _frameCounter = 0;
                    _state = 93;
                    if (_verticalSpeed < 36) {
                        _sprite = 49;
                        _periodCounter = -4;
                    } else {
                        if ((NextRandom() & 3) == 0) {
                            _sprite = 48;
                        } else {
                            _sprite = 42;
                        }
                        _periodCounter = -12;
                    }
                    PresentMain(_x, _y, _sprite);
                    break;
                } else {
                    _verticalSpeed = _verticalSpeed * 2 / -3;
                    _hasBounced = 1;
                }
            }
            if (_fallVariant != 0) {
                _sprite = _sprite == 4 ? 5 : 4;
            } else {
                _sprite = 42;
            }
            PresentMain(_x, _y, _sprite);
            break;
        case 93:
            if (_periodCounter++ < 1) {
                break;
            }
            _periodCounter = 0;
            if (_fallVariant != 0) {
                _state = 11;
                _sprite = 2;
                break;
            }
            if (_frameCounter == 0) {
                _sprite = 13;
            } else if (_frameCounter == 1) {
                if (_direction > 0) {
                    _sprite = 12;
                } else {
                    _sprite = 14;
                }
            } else if (_frameCounter == 2) {
                _sprite = 3;
            }
            PresentMain(_x, _y, _sprite);
            if (_frameCounter++ >= 2) {
                _state = 11;
            }
            break;
        case 94:
            _gravityEnabled = 1;
            _verticalSpeed = 0;
            _horizontalSpeed = -(_direction * 8);
            _fallVariant = 1;
            _state = 99;
            goto DispatchState;
        case 95:
            _gravityEnabled = 1;
            _verticalSpeed = 0;
            _horizontalSpeed = -(_direction * 3);
            _fallVariant = 1;
            _state = 99;
            goto DispatchState;
        case 96:
            _gravityEnabled = 1;
            _verticalSpeed = 0;
            _horizontalSpeed = 0;
            _fallVariant = 0;
            _state = 99;
            goto DispatchState;
        case 97:
            _gravityEnabled = 1;
            _verticalSpeed = 0;
            _horizontalSpeed = 0;
            _fallVariant = 1;
            _state = 99;
            goto DispatchState;
        case 98:
            _gravityEnabled = 1;
            _verticalSpeed = 0;
            _horizontalSpeed = 0;
            _fallVariant = 2;
            _state = 99;
            goto DispatchState;
        case 99:
            RefreshWindowSnapshot();
            _verticalSpeed += 4;
            _previousY = _y;
            _x += _horizontalSpeed;
            _y += _verticalSpeed;
            if (_previousY > SceneBottom) {
                PresentMain(_x, _y, _sprite);
                _state = 0;
                break;
            }
            if ((var_4 = FindLandingEdge(out _landingWindow, 40 + _y, 40 + _previousY, _x, 40 + _x)) != NoCollision) {
                if (!IsSupportValid(_landingWindow)) {
                    PresentMain(_x, _y, _sprite);
                    _state = 0;
                    break;
                }
                GetSupportRect(_landingWindow, out _landingRect);
                _y = var_4 - 40;
                if (_fallVariant == 3) {
                    _sprite = 66;
                    PresentMain(_x, _y, _sprite);
                    _state = 29;
                    break;
                }
                if (_verticalSpeed < 64 && _hasBounced == 0 || _verticalSpeed < 8) {
                    if (_landingWindow != IntPtr.Zero) {
                        PlaceWindowBehind(MainWindowHandle, _landingWindow);
                    }
                    _hasBounced = 0;
                    _frameCounter = 0;
                    _state = 100;
                    if (_verticalSpeed < 36) {
                        _sprite = 49;
                        _periodCounter = -4;
                    } else {
                        if ((NextRandom() & 3) == 0) {
                            _sprite = 48;
                        } else {
                            _sprite = 42;
                        }
                        _periodCounter = -10;
                    }
                    if (_fallVariant == 2) {
                        if (_verticalSpeed < 36) {
                            _sprite = 41;
                        } else {
                            _sprite = 45;
                        }
                    }
                    PresentMain(_x, _y, _sprite);
                    break;
                } else {
                    if ((NextRandom() & 7) == 0 && _hasBounced == 0) {
                        _hasBounced = 0;
                        _frameCounter = 0;
                        _state = 100;
                        _sprite = 48;
                        _periodCounter = -12;
                        if (_fallVariant == 2) {
                            _sprite = 45;
                        }
                        PresentMain(_x, _y, _sprite);
                        break;
                    }
                    _verticalSpeed = _verticalSpeed * 2 / -3;
                    _hasBounced = 1;
                }
            }
            if (_fallVariant == 2) {
                _sprite = _sprite == 40 ? 41 : 40;
            } else if (_fallVariant == 1) {
                _sprite = _sprite == 4 ? 5 : 4;
            } else if (_fallVariant == 0) {
                _sprite = 42;
            } else {
                _sprite = CollisionFrames[_collisionFrame];
                _collisionFrame += 1;
                if (_sprite == 66) {
                    _collisionFrame -= 1;
                }
            }
            if (_fallVariant == 3 && FindSheepCollision(_x, _x - _horizontalSpeed) != NoCollision) {
                _direction = -_direction;
                _state = 30;
                break;
            }
            PresentMain(_x, _y, _sprite);
            break;
        case 100:
            if (_periodCounter++ < 1) {
                break;
            }
            _periodCounter = 0;
            if (_fallVariant == 1) {
                _state = 11;
                _sprite = 2;
                break;
            }
            if (_fallVariant == 2) {
                _frameCounter = 0;
                _state = 101;
                break;
            }
            if (_frameCounter == 0) {
                _sprite = 13;
            } else if (_frameCounter == 1) {
                if (_direction > 0) {
                    _sprite = 12;
                } else {
                    _sprite = 14;
                }
            } else if (_frameCounter == 2) {
                _sprite = 3;
            }
            PresentMain(_x, _y, _sprite);
            if (_frameCounter++ >= 2) {
                _state = 11;
            }
            break;
        case 101:
            if (_periodCounter++ < 1) {
                break;
            }
            _periodCounter = 0;
            if (_frameCounter == 0) {
                _sprite = 31;
                _periodCounter = -8;
            } else if (_frameCounter == 2) {
                _sprite = 3;
            }
            PresentMain(_x, _y, _sprite);
            if (_frameCounter++ >= 6) {
                _state = 11;
            }
            break;
        case 102:
            StopSound();
            _frameCounter = 6;
            _sprite = 3;
            _actionVariant = 0;
            if (NextRandom() % 3 == 0) {
                _actionVariant = 1;
            }
            _state = 103;
            goto case 103;
        case 103:
            if (_actionVariant != 0) {
                _sprite = _sprite == 50 ? 51 : 50;
            } else {
                _sprite = _sprite == 4 ? 5 : 4;
            }
            PresentMain(_x, _y, _sprite);
            if (_frameCounter-- <= 0) {
                _state = 97;
            }
            break;
        case 104:
            _fallVariant = 0;
            _state = 106;
            goto DispatchState;
        case 105:
            _fallVariant = 1;
            _state = 106;
            goto DispatchState;
        case 106:
            if (_fallVariant == 0) {
                var_14.X = _x;
                var_14.Y = _y + 39;
                hitWindow = WindowAt(var_14);
                var_14.X = _x + 39;
                var_8 = WindowAt(var_14);
                if (hitWindow == MainWindowHandle && var_8 == MainWindowHandle) {
                    RaiseWindow(MainWindowHandle);
                } else if (hitWindow == MainWindowHandle) {
                    PlaceWindowBehind(MainWindowHandle, var_8);
                } else {
                    PlaceWindowBehind(MainWindowHandle, hitWindow);
                }
                _sprite = 81;
            } else {
                _sprite = 78;
            }
            PresentMain(_x, _y, _sprite);
            _state = 107;
            _frameCounter = 0;
            break;
        case 107:
            _sprite = BlinkFrames[4 - _fallVariant, _frameCounter];
            PresentMain(_x, _y, _sprite);
            _frameCounter += 1;
            if (_frameCounter > 7) {
                if (_fallVariant != 0) {
                    if ((NextRandom() & 1) == 0) {
                        _state = 111;
                    } else {
                        _state = 109;
                    }
                } else {
                    if ((NextRandom() & 1) == 0) {
                        _state = 111;
                    } else {
                        _state = 108;
                    }
                }
            }
            break;
        case 108:
            if (_periodCounter++ < 10) {
                break;
            }
            _periodCounter = 0;
            _sprite = 3;
            PresentMain(_x, _y, _sprite);
            _state = 1;
            break;
        case 109:
            _horizontalSpeed = -(_direction * 14);
            _sprite = 23;
            _x += _horizontalSpeed;
            PresentMain(_x, _y, _sprite);
            _state = 95;
            _frameCounter = 0;
            break;
        case 110:
            _x += _horizontalSpeed;
            _horizontalSpeed += _direction;
            PresentMain(_x, _y, _sprite);
            if (_frameCounter++ > 3) {
                _state = 95;
            }
            break;
        case 111:
            if (_fallVariant != 0) {
                _x += _direction * -26;
                _y += 35;
                _direction = -_direction;
            } else {
                _actionVariant = NextRandom() % 2;
                if (_actionVariant != 0) {
                    _y += 36;
                } else {
                    _y += 20;
                }
            }
            _frameCounter = 0;
            _state = 112;
            goto case 112;
        case 112:
            if (_frameCounter == 0) {
                if (_periodCounter++ < 10) {
                    break;
                }
                _periodCounter = 0;
            } else {
                if (_periodCounter++ < 1) {
                    break;
                }
                _periodCounter = 0;
            }
            if (_fallVariant != 0) {
                _sprite = _sprite == 40 ? 41 : 40;
            } else {
                _sprite = HangFrames[_actionVariant, _frameCounter % 4];
            }
            PresentMain(_x, _y, _sprite);
            _frameCounter += 1;
            if (_frameCounter > 12) {
                if (_fallVariant != 0) {
                    _state = 98;
                } else {
                    _state = 96;
                }
            }
            break;
        case 113:
            _sleeping = 1;
            _sprite = 6;
            PresentMain(_x, _y, _sprite);
            _state = 114;
            break;
        case 114:
            if (_periodCounter++ < 1) {
                break;
            }
            _periodCounter = 0;
            _sprite += 1;
            PresentMain(_x, _y, _sprite);
            if (_sprite == 8) {
                _sprite = 0;
                _state = 115;
            }
            CheckSupport(0);
            break;
        case 115:
            CheckSupport(0);
            if (_periodCounter++ < 4) {
                break;
            }
            _periodCounter = 0;
            _sprite = _sprite == 0 ? 1 : 0;
            PresentMain(_x, _y, _sprite);
            break;
        case 116:
            _x = SceneRight;
            _y = ScenePointY(7, 8);
            _sprite = 4;
            _direction = 1;
            PresentMain(_x, _y, _sprite);
            _state = 117;
            break;
        case 117:
            if (_periodCounter++ < 1) {
                break;
            }
            _periodCounter = 0;
            _x -= _direction * 16;
            _sprite = _sprite == 4 ? 5 : 4;
            PresentMain(_x, _y, _sprite);
            if (SceneCenterX - 20 >= _x) {
                _state = 118;
            }
            break;
        case 118:
            ShowCompanion();
            _companionDirection = -1;
            _companionX = SceneLeft - 40;
            _companionY = ScenePointY(1, 8);
            _companionSprite = 154;
            _frameCounter = 0;
            _state = 119;
            _mainAboveCompanion = 0;
            RaiseWindow(MainWindowHandle);
            RaiseWindow(CompanionWindowHandle);
            break;
        case 119:
            if (_frameCounter != 0) {
                _sprite = BlinkFrames[2, _frameCounter];
                PresentMain(_x, _y, _sprite);
                _frameCounter += 1;
                if (_frameCounter > 7) {
                    _frameCounter = 0;
                }
            } else {
                _sprite = 73;
                PresentMain(_x, _y, _sprite);
                if (NextRandom() % 20 == 0) {
                    _frameCounter = 1;
                }
            }
            if (_periodCounter++ < 1) {
                break;
            }
            _periodCounter = 0;
            _companionX -= _companionDirection * 16;
            _companionSprite = _companionSprite == 154 ? 155 : 154;
            PresentCompanion(_companionX, _companionY, _companionSprite);
            if (_companionX > _x) {
                _direction = -1;
                PresentMain(_x, _y, _sprite);
            }
            if (_companionX > SceneRight) {
                HideCompanion();
                _state = 120;
            }
            break;
        case 120:
            if (_periodCounter++ < 1) {
                break;
            }
            _periodCounter = 0;
            _x -= _direction * 16;
            _sprite = _sprite == 4 ? 5 : 4;
            PresentMain(_x, _y, _sprite);
            if (_x > SceneRight) {
                _state = 1;
            }
            break;
        case 121:
            _x = SceneRight;
            _y = ScenePointY(7, 8);
            _sprite = 4;
            _direction = 1;
            ShowCompanion();
            _companionDirection = -1;
            _companionX = SceneLeft - 40;
            _companionY = ScenePointY(7, 8);
            _companionSprite = 154;
            PresentMain(_x, _y, _sprite);
            PresentCompanion(_companionX, _companionY, _companionSprite);
            _state = 122;
            _mainAboveCompanion = 0;
            RaiseWindow(MainWindowHandle);
            RaiseWindow(CompanionWindowHandle);
            break;
        case 122:
            if (_periodCounter++ < 1) {
                break;
            }
            _periodCounter = 0;
            _x -= _direction * 16;
            _sprite = _sprite == 4 ? 5 : 4;
            _companionX -= _companionDirection * 16;
            _companionSprite = _companionSprite == 154 ? 155 : 154;
            if (_x - _companionX <= 46) {
                _x = SceneCenterX + 3;
                _companionX = SceneCenterX - 43;
                _sprite = 3;
                _companionSprite = 157;
                PresentMain(_x, _y, _sprite);
                PresentCompanion(_companionX, _companionY, _companionSprite);
                _frameCounter = 0;
                _state = 123;
            } else {
                PresentMain(_x, _y, _sprite);
                PresentCompanion(_companionX, _companionY, _companionSprite);
            }
            break;
        case 123:
            if (_periodCounter++ < 3) {
                break;
            }
            _periodCounter = 0;
            _sprite = _frameCounter + 127;
            _frameCounter += 1;
            PresentMain(_x, _y, _sprite);
            if (_frameCounter >= 4) {
                _state = 124;
            }
            break;
        case 124:
            if (_periodCounter++ < 4) {
                break;
            }
            _periodCounter = 0;
            _fadeStep += 1;
            if (_fadeStep > 8) {
                HideCompanion();
                _frameCounter = 0;
                _state = 125;
            }
            break;
        case 125:
            _sprite = MerryFrames[_frameCounter];
            _frameCounter += 1;
            if (_sprite == 0) {
                _state = 1;
                break;
            }
            PresentMain(_x, _y, _sprite);
            break;
        case 126:
            _x = SceneRight;
            _y = ScenePointY(7, 8);
            _sprite = 4;
            _direction = 1;
            ShowCompanion();
            _companionDirection = 1;
            _companionX = SceneRight + 46;
            _companionY = ScenePointY(7, 8);
            _companionSprite = 154;
            PresentMain(_x, _y, _sprite);
            PresentCompanion(_companionX, _companionY, _companionSprite);
            _state = 127;
            _mainAboveCompanion = 0;
            RaiseWindow(MainWindowHandle);
            RaiseWindow(CompanionWindowHandle);
            break;
        case 127:
            if (_periodCounter++ < 1) {
                break;
            }
            _periodCounter = 0;
            _x -= _direction * 16;
            _sprite = _sprite == 4 ? 5 : 4;
            _companionX -= _companionDirection * 16;
            _companionSprite = _companionSprite == 154 ? 155 : 154;
            if (_companionX < SceneLeft - 40) {
                HideCompanion();
                _state = 1;
            } else {
                PresentMain(_x, _y, _sprite);
                PresentCompanion(_companionX, _companionY, _companionSprite);
            }
            break;
        case 128:
            _x = SceneRight;
            _y = ScenePointY(7, 8);
            _sprite = 4;
            _direction = 1;
            PresentMain(_x, _y, _sprite);
            _state = 129;
            break;
        case 129:
            if (_periodCounter++ < 1) {
                break;
            }
            _periodCounter = 0;
            _x -= _direction * 16;
            _sprite = _sprite == 4 ? 5 : 4;
            PresentMain(_x, _y, _sprite);
            if (SceneCenterX - 20 >= _x) {
                _state = 130;
            }
            break;
        case 130:
            ShowCompanion();
            _companionDirection = -1;
            _companionX = SceneLeft - 40;
            _companionY = ScenePointY(1, 8);
            _companionSprite = 158;
            _frameCounter = 0;
            _state = 131;
            _mainAboveCompanion = 0;
            RaiseWindow(MainWindowHandle);
            RaiseWindow(CompanionWindowHandle);
            break;
        case 131:
            if (_frameCounter != 0) {
                _sprite = BlinkFrames[2, _frameCounter];
                PresentMain(_x, _y, _sprite);
                _frameCounter += 1;
                if (_frameCounter > 7) {
                    _frameCounter = 0;
                }
            } else {
                _sprite = 73;
                PresentMain(_x, _y, _sprite);
                if (NextRandom() % 20 == 0) {
                    _frameCounter = 1;
                }
            }
            _companionX -= _companionDirection * 16;
            if (_companionSprite == 161) {
                _companionSprite = 158;
            } else {
                _companionSprite += 1;
            }
            if (_companionX > _x) {
                _companionX = _x;
                _companionSprite = 162;
                _state = 132;
            }
            PresentCompanion(_companionX, _companionY, _companionSprite);
            break;
        case 132:
            _sprite = 73;
            PresentMain(_x, _y, _sprite);
            _companionBeamHeight += 40;
            if (_y - _companionY - 40 <= _companionBeamHeight) {
                _companionBeamHeight = _y - _companionY - 40;
                _companionBeamHeight -= 20;
                _state = 133;
            }
            PresentCompanion(_companionX, _companionY, _companionSprite);
            if (_periodCounter++ < 1) {
                break;
            }
            _periodCounter = 0;
            if (_companionSprite == 165) {
                _companionSprite = 162;
            } else {
                _companionSprite += 1;
            }
            break;
        case 133:
            _companionBeamHeight -= 20;
            if (_companionBeamHeight <= 0) {
                _companionBeamHeight = 0;
                _y = _companionY + 40;
                _state = 134;
                _sprite = _sprite == 4 ? 5 : 4;
                PresentMain(_x, _y, _sprite);
                _companionSprite = 158;
                PresentCompanion(_companionX, _companionY, _companionSprite);
                break;
            }
            PresentCompanion(_companionX, _companionY, _companionSprite);
            _sprite = _sprite == 4 ? 5 : 4;
            _y -= 20;
            PresentMain(_x, _y, _sprite);
            if (_periodCounter++ < 1) {
                break;
            }
            _periodCounter = 0;
            if (_companionSprite == 165) {
                _companionSprite = 162;
            } else {
                _companionSprite += 1;
            }
            break;
        case 134:
            _x = SceneLeft - 80;
            PresentMain(_x, _y, _sprite);
            _companionX -= _companionDirection * 16;
            if (_companionSprite == 161) {
                _companionSprite = 158;
            } else {
                _companionSprite += 1;
            }
            if (_companionX > SceneRight) {
                HideCompanion();
                StopSound();
                _state = 1;
                break;
            }
            PresentCompanion(_companionX, _companionY, _companionSprite);
            break;
        case 135:
            _companionY = ScenePointY(7, 8);
            _direction = -1;
            _x = SceneLeft - 40;
            _y = ScenePointY(1, 8);
            _sprite = 158;
            _frameCounter = 0;
            _state = 136;
            break;
        case 136:
            _x -= _direction * 16;
            if (_sprite == 161) {
                _sprite = 158;
            } else {
                _sprite += 1;
            }
            if (SceneCenterX - 20 < _x) {
                _x = SceneCenterX - 20;
                _sprite = 162;
                _state = 137;
            }
            PresentMain(_x, _y, _sprite);
            break;
        case 137:
            _beamHeight += 40;
            if (_companionY - _y - 40 <= _beamHeight) {
                _beamHeight = _companionY - _y - 40;
                _state = 138;
            }
            PresentMain(_x, _y, _sprite);
            if (_periodCounter++ < 1) {
                break;
            }
            _periodCounter = 0;
            if (_sprite == 165) {
                _sprite = 162;
            } else {
                _sprite += 1;
            }
            break;
        case 138:
            if (_periodCounter++ < 4) {
                break;
            }
            _periodCounter = 0;
            ShowCompanion();
            _companionX = _x;
            _companionSprite = 167;
            PresentCompanion(_companionX, _companionY, _companionSprite);
            PresentMain(_x, _y, _sprite);
            _state = 139;
            _mainAboveCompanion = 0;
            RaiseWindow(MainWindowHandle);
            RaiseWindow(CompanionWindowHandle);
            break;
        case 139:
            if (_beamHeight != 0) {
                _beamHeight -= 40;
                if (_beamHeight <= 0) {
                    _sprite = 158;
                    _beamHeight = 0;
                }
                if (_sprite == 165) {
                    _sprite = 162;
                } else {
                    _sprite += 1;
                }
            } else {
                _x -= _direction * 16;
                if (_sprite == 161) {
                    _sprite = 158;
                } else {
                    _sprite += 1;
                }
            }
            if (_x > SceneRight) {
                _state = 140;
            }
            PresentMain(_x, _y, _sprite);
            if (_periodCounter++ < 1) {
                break;
            }
            _periodCounter = 0;
            _companionSprite = _companionSprite == 167 ? 168 : 167;
            PresentCompanion(_companionX, _companionY, _companionSprite);
            break;
        case 140:
            _companionSprite = 166;
            PresentCompanion(_companionX, _companionY, _companionSprite);
            if (_periodCounter++ < 1) {
                break;
            }
            _periodCounter = 0;
            _fadeStep += 1;
            if (_fadeStep > 8) {
                HideCompanion();
                StopSound();
                _state = 1;
            }
            break;
        case 141:
            break;
        case 142:
            _x = SceneLeft - 80;
            _y = ScenePointY(1, 8);
            PresentMain(_x, _y, _sprite);
            ShowCompanion();
            _companionDirection = -1;
            _companionX = SceneLeft - 40;
            _companionY = ScenePointY(7, 8);
            _companionSprite = 158;
            _frameCounter = 0;
            _state = 143;
            _mainAboveCompanion = 0;
            RaiseWindow(MainWindowHandle);
            RaiseWindow(CompanionWindowHandle);
            break;
        case 143:
            _companionX -= _companionDirection * 16;
            if (_companionSprite == 161) {
                _companionSprite = 158;
            } else {
                _companionSprite += 1;
            }
            if (SceneLeft + SceneHeight / 8 < _companionX) {
                _companionX = SceneLeft + SceneHeight / 8;
                _x = SceneRight;
                _y = _companionY;
                _sprite = 4;
                _direction = 1;
                _state = 144;
            }
            PresentCompanion(_companionX, _companionY, _companionSprite);
            break;
        case 144:
            if (_periodCounter++ < 1) {
                break;
            }
            _periodCounter = 0;
            if (_companionSprite == 161) {
                _companionSprite = 158;
            } else {
                _companionSprite += 1;
            }
            PresentCompanion(_companionX, _companionY, _companionSprite);
            _x -= _direction * 16;
            _sprite = _sprite == 4 ? 5 : 4;
            PresentMain(_x, _y, _sprite);
            if (_companionX + 40 >= _x) {
                _x = SceneLeft - 80;
                PresentMain(_x, _y, _sprite);
                _state = 145;
            }
            break;
        case 145:
            _companionY -= 40;
            if (_companionSprite == 161) {
                _companionSprite = 158;
            } else {
                _companionSprite += 1;
            }
            if (_companionY < SceneTop - 40) {
                HideCompanion();
                StopSound();
                _state = 1;
                break;
            }
            PresentCompanion(_companionX, _companionY, _companionSprite);
            break;
        case 146:
            break;
        case 147:
            ShowCompanion();
            _retainCompanion = 1;
            _companionDirection = 1;
            _companionSprite = 146;
            _frameCounter = 0;
            _x = SceneRight;
            _y = SceneTop - 40;
            _direction = 1;
            _horizontalSpeed = SceneWidth / -96;
            _verticalSpeed = SceneHeight / 96;
            _companionX = _horizontalSpeed * 92 + SceneRight;
            _companionY = SceneTop + _verticalSpeed * 92 - 20;
            _state = 148;
            _mainAboveCompanion = 1;
            RaiseWindow(CompanionWindowHandle);
            RaiseWindow(MainWindowHandle);
            goto case 148;
        case 148:
            if (_periodCounter++ < 0) {
                break;
            }
            _periodCounter = 0;
            PresentCompanion(_companionX, _companionY, _companionSprite);
            _x += _horizontalSpeed;
            _y += _verticalSpeed;
            _sprite = BurnFrames[_frameCounter / 3];
            _frameCounter += 1;
            if (_sprite == 0) {
                _frameCounter -= 1;
            }
            if (_sprite == 0 || _sprite == 144 || _sprite == 145) {
                _sprite = _sprite == 144 ? 145 : 144;
            }
            if (_sprite == 137 || _sprite == 138) {
                _sprite = _sprite == 137 ? 138 : 137;
            }
            PresentMain(_x, _y, _sprite);
            if (_companionX + 10 > _x || _companionY + 20 < _y) {
                _frameCounter = 0;
                _state = 149;
                _sprite = 173;
                PresentMain(_x, _y, _sprite);
                break;
            }
            break;
        case 149:
            if (_periodCounter++ < 1) {
                break;
            }
            _periodCounter = 0;
            _x = SceneLeft - 80;
            PresentMain(_x, _y, _sprite);
            _companionSprite = SplashFrames[_frameCounter];
            _frameCounter += 1;
            if (_companionSprite == 0) {
                _x = _companionX;
                _y = _companionY;
                _frameCounter = 0;
                PlaySound(108, 0, 0);
                _state = 150;
                break;
            }
            PresentCompanion(_companionX, _companionY, _companionSprite);
            break;
        case 150:
            _sprite = 169;
            _sprite = BathExitFrames[_frameCounter];
            _frameCounter += 1;
            if (_sprite == 0) {
                _sprite = 3;
                _state = 151;
                break;
            }
            if (_sprite >= 81 && _sprite <= 83) {
                PresentMain(_x, _y - 20, _sprite);
            } else {
                PresentMain(_x, _y, _sprite);
            }
            break;
        case 151:
            if (_periodCounter++ < 1) {
                break;
            }
            _periodCounter = 0;
            _x -= _direction * 6;
            _sprite = _sprite == 2 ? 3 : 2;
            PresentMain(_x, _y, _sprite);
            if (_x < SceneLeft - 40) {
                HideCompanion();
                _state = 1;
                break;
            }
            break;
        case 152:
            break;
        default:
            break;
        }
    }

    private void TurnAtBoundary()
    {
        EngineRect area = default;
        GetWalkBounds(_x, _y, out area);
        if (_direction > 0 && _x < area.Left) {
            _state = 24;
        }
        if (_direction < 0 && area.Right - 40 < _x) {
            _state = 24;
        }
        if (_direction > 0 && area.Right - 40 > _x && NextRandom() % 20 == 0) {
            _state = 24;
        }
        if (_direction < 0 && _x > area.Left && NextRandom() % 20 == 0) {
            _state = 24;
        }
    }

    private void CheckRunningBoundary(bool arg_0)
    {
        EngineRect area = default;
        GetWalkBounds(_x, _y, out area);
        if (_gravityEnabled == 0) {
            if (_direction > 0 && _x < area.Left) {
                _state = 30;
            }
            if (_direction < 0 && area.Right - 40 < _x) {
                _state = 30;
            }
        }
        if (arg_0) {
            if (_direction > 0 && area.Right - 80 > _x && NextRandom() % 20 == 0) {
                _state = 24;
            }
            if (_direction < 0 && _x > area.Left + 40 && NextRandom() % 20 == 0) {
                _state = 24;
            }
        }
    }

    private void FinishTimedAction()
    {
        if (_frameCounter-- <= 0) {
            _state = 42;
        }
    }

    private void CheckSupport(int arg_0)
    {
        EngineRect var_8 = default;
        if (_gravityEnabled == 0) {
            return;
        }
        if (_landingWindow != IntPtr.Zero) {
            if (!IsSupportValid(_landingWindow)) {
                if (arg_0 == 2) {
                    _state = 94;
                } else {
                    _state = 102;
                }
                return;
            }
            GetSupportRect(_landingWindow, out var_8);
            if (var_8.Top > _landingRect.Top || _x + 40 < var_8.Left || var_8.Right < _x) {
                if (arg_0 == 2) {
                    _state = 94;
                } else {
                    _state = 102;
                }
                return;
            }
            if (var_8.Top < _landingRect.Top) {
                _y = var_8.Top - 40;
                _landingRect.Top = var_8.Top;
                _landingRect.Bottom = var_8.Bottom;
                _landingRect.Left = var_8.Left;
                _landingRect.Right = var_8.Right;
                PresentMain(_x, _y, _sprite);
                return;
            }
            if (arg_0 == 1) {
                if (_x + 8 < var_8.Left && _direction > 0) {
                    _state = 105;
                    _x = var_8.Left - 10;
                    return;
                }
                if (_x + 32 >= var_8.Right && _direction < 0) {
                    _state = 105;
                    _x = var_8.Right - 30;
                    return;
                }
                if (NextRandom() % 20 - 1 == 0 && SceneBottom - _y > 100) {
                    _state = 104;
                    return;
                }
            }
            if (arg_0 == 2) {
                if (_x + 32 < var_8.Left || _x + 8 > var_8.Right) {
                    _state = 94;
                    return;
                }
            }
        }
        if (40 + _x < SceneLeft || _x > SceneRight) {
            _state = 0;
            return;
        }
    }

    private void CheckClimbingWindow()
    {
        EngineRect var_8 = default;
        if (_gravityEnabled == 0) {
            return;
        }
        if (_landingWindow != IntPtr.Zero) {
            if (!IsSupportValid(_landingWindow)) {
                _state = 102;
                return;
            }
            GetSupportRect(_landingWindow, out var_8);
            if (var_8.Right < _landingRect.Right && _direction > 0 || var_8.Left > _landingRect.Left && _direction < 0) {
                _state = 102;
                return;
            }
            if (var_8.Right > _landingRect.Right && _direction > 0 || var_8.Left < _landingRect.Left && _direction < 0) {
                if (_direction > 0) {
                    _x = var_8.Right + 10;
                } else {
                    _x = var_8.Left - 50;
                }
                PresentMain(_x, _y, _sprite);
                _state = 102;
                return;
            }
        }
    }

    private void CheckSheepCollision(int arg_0, int arg_2, int arg_4)
    {
        if (arg_2 < arg_0) {
            arg_0 += 40;
            arg_2 = arg_0 - 80;
        } else {
            arg_2 = arg_0 + 80;
        }
        if (FindPeerEdge(arg_0, arg_2, _y, _y + 40) != NoCollision) {
            if (arg_4 == 1) {
                _state = 24;
            }
            if (arg_4 == 2) {
                _state = 30;
            }
        }
    }

    private int FindSheepCollision(int arg_0, int arg_2)
    {
        if (arg_2 < arg_0) {
            arg_0 += 40;
            arg_2 = arg_0 - 80;
        } else {
            arg_2 = arg_0 + 80;
        }
        return FindPeerEdge(arg_0, arg_2, _y, _y + 40);
    }

    private void React(int arg_0)
    {
        switch (arg_0) {
        case 0:
            _state = 1;
            if (_gravityEnabled != 0) {
                _state = 97;
            }
            break;
        case 1:
            _state = 81;
            break;
        case 2:
            _state = 97;
            break;
        case 3:
            _pendingSleepState = 113;
            break;
        case 4:
            _state = 56;
            break;
        default:
            break;
        }
    }
}