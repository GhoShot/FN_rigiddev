using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Serializing;
using FishNet.Transporting;
using System;
using UnityEngine;

/*
* 
* See TransformPrediction.cs for more detailed notes.
* 
*/

#region Types.

[Flags]
public enum ExtraOper : byte
{
    NONE = 0,
    FREEZE      =   1 << 0,
    FLOAT       =   1 << 2,
    TELEPORT    =   1 << 3,
    IMPULSE     =   1 << 4,
}

public struct MoveData : IReplicateData
{
    public uint Generated___Tick; //Add this field.
    public ExtraOper ExOp;
    public float Horizontal;
    public float Vertical;

    private uint _tick;
    public void Dispose() { }
    public uint GetTick() => _tick;
    public void SetTick(uint value) => _tick = value;
}



public struct ReconcileData : IReconcileData
{
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Velocity;
    public Vector3 AngularVelocity;
    public ReconcileData(Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angularVelocity)
    {
        Position = position;
        Rotation = rotation;
        Velocity = velocity;
        AngularVelocity = angularVelocity;
        _tick = 0;
    }

    private uint _tick;
    public void Dispose() { }
    public uint GetTick() => _tick;
    public void SetTick(uint value) => _tick = value;
}

//public static class MoveDataSerializer
//{
//    public static void WriteMoveData(this Writer writer, MoveData value)
//    {
//        writer.WriteUInt32(value.Generated___Tick, AutoPackType.Unpacked);
//        writer.WriteSingle(value.Horizontal);
//        writer.WriteSingle(value.Vertical);
//        writer.WriteByte((byte)value.ExOp);
//    }

//    public static MoveData ReadMoveData(this Reader reader, MoveData value)
//    {
//        var res = new MoveData();
//        res.Generated___Tick = reader.ReadUInt32(AutoPackType.Unpacked);
//        res.Horizontal = reader.ReadSingle();
//        res.Vertical = reader.ReadSingle();
//        res.ExOp = (ExtraOper)reader.ReadByte();
//        return res;
//    }
//}

#endregion


namespace FishNet.Example.Prediction.CharacterControllers
{
    public class RigidDev : NetworkBehaviour
    {

        #region Serialized.
        [SerializeField]
        private float _moveRate;
        [SerializeField]
        private float _jumpForce;
        [SerializeField] private float _mass;
        [SerializeField] private float _drag;
        [SerializeField] private Transform graphTrans;
        #endregion

        #region Private.
        private Rigidbody _rigidbody;
        private Transform _cameraPos;
        #endregion


        private JoyStick _joy;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _cameraPos = Camera.main.transform;
            _joy = JoyStick.Instance;
        }
        private void Start()
        {
        }

        private bool _isRespawn = false;
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                _isRespawn = true;
            }
            if(Input.GetKeyDown(KeyCode.S))
            {
                _isImpulse = true;
            }
        }

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            _rigidbody.mass = _mass;
            _rigidbody.drag = _drag;
            InstanceFinder.TimeManager.OnTick += TimeManager_OnTick;
            InstanceFinder.TimeManager.OnPostTick += TimeManager_OnPostTick;
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            if (InstanceFinder.TimeManager != null)
            {
                InstanceFinder.TimeManager.OnTick -= TimeManager_OnTick;
                InstanceFinder.TimeManager.OnPostTick -= TimeManager_OnPostTick;
            }
        }

        private void OnDestroy()
        {
            if (InstanceFinder.TimeManager != null)
            {
                InstanceFinder.TimeManager.OnTick -= TimeManager_OnTick;
                InstanceFinder.TimeManager.OnPostTick -= TimeManager_OnPostTick;
            }
        }

#if !PREDICTION_V2

        private void CheckInput(out MoveData md)
        {
            md = default;

            float horizontal = _joy.Input.x;
            float vertical = _joy.Input.y;

            md = new MoveData()
            {
                Horizontal = horizontal,
                Vertical = vertical
            };
        }

        private void TimeManager_OnTick()
        {
            if (base.IsOwner)
            {
                Reconciliation(default, false);
                CheckInput(out MoveData md);
                Move(md, false);
            }
            if (base.IsServer)
            {
                Move(default, true);
                ReconcileData rd = new ReconcileData(transform.position, transform.rotation);
                Reconciliation(rd, true);
            }
        }

        private void TimeManager_OnPostTick()
        {
        }


        [Replicate]
        private void Move(MoveData md, bool asServer, Channel channel = Channel.Unreliable, bool replaying = false)
        {

        }

        [Reconcile]
        private void Reconciliation(ReconcileData rd, bool asServer, Channel channel = Channel.Unreliable)
        {

        }
#else


        private void TimeManager_OnPostTick()
        {
            if (IsServer)
            {
                ReconcileData rd = new ReconcileData(transform.position, transform.rotation, _rigidbody.velocity, _rigidbody.angularVelocity);
                Reconciliation(rd);
            }
        }

        private void TimeManager_OnTick()
        {
            var md = BuildMove();
            Move(md);
        }

        private MoveData? _lastMoveData;

        [ReplicateV2]
        private void Move(MoveData md, ReplicateState state = ReplicateState.Invalid, Channel channel = Channel.Unreliable)
        {
            //if (!base.IsOwner && !base.IsServer)
            //{
            //    if (state == ReplicateState.UserCreated || state == ReplicateState.ReplayedUserCreated)
            //    {
            //        _lastMoveData = md;
            //    }
            //    else
            //    {
            //        if (_lastMoveData != null)
            //        {
            //            uint tick = md.GetTick();
            //            md = _lastMoveData.Value;
            //            md.SetTick(tick);
            //        }
            //    }
            //}
            if (IsServer && _joy.Input != Vector2.zero)
            {
                md = BuildMove();
            }
            RigidMove(md);
        }

        [Server(Logging = Managing.Logging.LoggingType.Off)]
        private void FixedUpdate()
        {
            return;
            if (_joy.Input == Vector2.zero) return;
            var md = BuildMove();
            RigidMove(md);
        }

        private void RigidMove(MoveData md)
        {
            if ((md.ExOp & ExtraOper.TELEPORT) != ExtraOper.NONE)
            {
                _rigidbody.position = Vector3.zero;
                _rigidbody.velocity = Vector3.zero;
                return;
            }

            Vector3 forces = new Vector3(md.Horizontal, 0f, md.Vertical) * _moveRate;
            _rigidbody.AddForce(forces);

            if ((md.ExOp & ExtraOper.IMPULSE) != ExtraOper.NONE)
                _rigidbody.AddForce(new Vector3(0f, _jumpForce, 0f), ForceMode.Impulse);
            if ((md.ExOp & ExtraOper.TELEPORT) != ExtraOper.NONE)
                _rigidbody.position = Vector3.zero;
            //Add gravity to make the object fall faster.
            _rigidbody.AddForce(Physics.gravity, ForceMode.Acceleration);
        }

        private bool _isJump;
        private bool _isTeleport;
        private bool _isImpulse;

        private ExtraOper GetExOP()
        {
            ExtraOper exOP = (_isImpulse ? ExtraOper.IMPULSE : ExtraOper.NONE) | (_isRespawn ? ExtraOper.TELEPORT : ExtraOper.NONE);
            _isJump = false;
            _isImpulse = false;
            _isRespawn = false;
            return exOP;
        }

        private MoveData BuildMove()
        {
            //if (!IsOwner) return default;
            if (_joy == null) return default;
            MoveData md = new MoveData()
            {
                Horizontal = _joy.Input.x,
                Vertical = _joy.Input.y,
                ExOp = GetExOP(),
            };
            _isRespawn = false;
            return md;
        }

        [ReconcileV2]
        private void Reconciliation(ReconcileData rd, Channel channel = Channel.Unreliable)
        {
            transform.position = rd.Position;
            transform.rotation = rd.Rotation;
            _rigidbody.velocity = rd.Velocity;
            _rigidbody.angularVelocity = rd.AngularVelocity;
            Debug.Log("Reconcil" + rd.Velocity);
        }

#endif


    }


}