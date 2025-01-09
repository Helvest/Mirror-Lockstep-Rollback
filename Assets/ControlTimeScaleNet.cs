using Mirror;
using UnityEngine;

public class ControlTimeScaleNet : NetworkBehaviour
{
    #region Fields

    [SerializeField] private ControlTimeScale _control;

    #endregion

    #region Init

    protected override void OnValidate()
    {
        if (_control == null)
        {
            TryGetComponent(out _control);
        }

        base.OnValidate();
    }

    #endregion

    #region Network

    public override void OnSerialize(NetworkWriter writer, bool initialState)
    {
        base.OnSerialize(writer, initialState);

        writer.Write(_control.divideTime);
        writer.Write(_control.multiplyTime);
        writer.Write(_control.targetFrameRate);
        writer.Write(_control.useRenderFrameInterval);
        writer.Write(_control.renderFrame);
    }

    public override void OnDeserialize(NetworkReader reader, bool initialState)
    {
        base.OnDeserialize(reader, initialState);

        _control.divideTime = reader.ReadInt();
        _control.multiplyTime = reader.ReadInt();
        _control.targetFrameRate = reader.ReadInt();
        _control.useRenderFrameInterval = reader.ReadBool();
        _control.renderFrame = reader.ReadInt();
    }

    #endregion
}