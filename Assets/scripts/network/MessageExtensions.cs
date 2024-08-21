using Riptide;
using UnityEngine;
using chARpackStructs;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Riptide.Utils
{
    public static class MessageExtensions
    {
        #region Vector2
        /// <inheritdoc cref="Add(Message, Vector2)"/>
        /// <remarks>Relying on the correct Add overload being chosen based on the parameter type can increase the odds of accidental type mismatches when retrieving data from a message. This method calls <see cref="Add(Message, Vector2)"/> and simply provides an alternative type-explicit way to add a <see cref="Vector2"/> to the message.</remarks>
        public static Message AddVector2(this Message message, Vector2 value) => Add(message, value);

        /// <summary>Adds a <see cref="Vector2"/> to the message.</summary>
        /// <param name="value">The <see cref="Vector2"/> to add.</param>
        /// <returns>The message that the <see cref="Vector2"/> was added to.</returns>
        public static Message Add(this Message message, Vector2 value)
        {
            message.AddFloat(value.x);
            message.AddFloat(value.y);
            return message;
        }

        /// <summary>Retrieves a <see cref="Vector2"/> from the message.</summary>
        /// <returns>The <see cref="Vector2"/> that was retrieved.</returns>
        public static Vector2 GetVector2(this Message message)
        {
            return new Vector2(message.GetFloat(), message.GetFloat());
        }
        #endregion

        #region Vector3
        /// <inheritdoc cref="Add(Message, Vector3)"/>
        /// <remarks>Relying on the correct Add overload being chosen based on the parameter type can increase the odds of accidental type mismatches when retrieving data from a message. This method calls <see cref="Add(Message, Vector3)"/> and simply provides an alternative type-explicit way to add a <see cref="Vector3"/> to the message.</remarks>
        public static Message AddVector3(this Message message, Vector3 value) => Add(message, value);

        /// <summary>Adds a <see cref="Vector3"/> to the message.</summary>
        /// <param name="value">The <see cref="Vector3"/> to add.</param>
        /// <returns>The message that the <see cref="Vector3"/> was added to.</returns>
        public static Message Add(this Message message, Vector3 value)
        {
            message.AddFloat(value.x);
            message.AddFloat(value.y);
            message.AddFloat(value.z);
            return message;
        }

        /// <summary>Retrieves a <see cref="Vector3"/> from the message.</summary>
        /// <returns>The <see cref="Vector3"/> that was retrieved.</returns>
        public static Vector3 GetVector3(this Message message)
        {
            return new Vector3(message.GetFloat(), message.GetFloat(), message.GetFloat());
        }
        #endregion

        #region Quaternion
        /// <inheritdoc cref="Add(Message, Quaternion)"/>
        /// <remarks>Relying on the correct Add overload being chosen based on the parameter type can increase the odds of accidental type mismatches when retrieving data from a message. This method calls <see cref="Add(Message, Quaternion)"/> and simply provides an alternative type-explicit way to add a <see cref="Quaternion"/> to the message.</remarks>
        public static Message AddQuaternion(this Message message, Quaternion value) => Add(message, value);

        /// <summary>Adds a <see cref="Quaternion"/> to the message.</summary>
        /// <param name="value">The <see cref="Quaternion"/> to add.</param>
        /// <returns>The message that the <see cref="Quaternion"/> was added to.</returns>
        public static Message Add(this Message message, Quaternion value)
        {
            message.AddFloat(value.x);
            message.AddFloat(value.y);
            message.AddFloat(value.z);
            message.AddFloat(value.w);
            return message;
        }

        /// <summary>Retrieves a <see cref="Quaternion"/> from the message.</summary>
        /// <returns>The <see cref="Quaternion"/> that was retrieved.</returns>
        public static Quaternion GetQuaternion(this Message message)
        {
            return new Quaternion(message.GetFloat(), message.GetFloat(), message.GetFloat(), message.GetFloat());
        }
        #endregion

        #region BondTerm
        public static Message AddBondTerm(this Message message, ForceField.BondTerm value) => Add(message, value);

        public static Message Add(this Message message, ForceField.BondTerm value)
        {
            message.AddUShort(value.Atom1);
            message.AddUShort(value.Atom2);
            message.AddFloat(value.kBond);
            message.AddFloat(value.eqDist);
            message.AddFloat(value.order);
            return message;
        }

        public static ForceField.BondTerm GetBondTerm(this Message message)
        {
            var term = new ForceField.BondTerm();
            term.Atom1 = message.GetUShort();
            term.Atom2 = message.GetUShort();
            term.kBond = message.GetFloat();
            term.eqDist = message.GetFloat();
            term.order = message.GetFloat();
            return term;
        }
        #endregion

        #region AngleTerm
        public static Message AddAngleTerm(this Message message, ForceField.AngleTerm value) => Add(message, value);

        public static Message Add(this Message message, ForceField.AngleTerm value)
        {
            message.AddUShort(value.Atom1);
            message.AddUShort(value.Atom2);
            message.AddUShort(value.Atom3);
            message.AddFloat(value.kAngle);
            message.AddFloat(value.eqAngle);
            return message;
        }

        public static ForceField.AngleTerm GetAngleTerm(this Message message)
        {
            var term = new ForceField.AngleTerm();
            term.Atom1 = message.GetUShort();
            term.Atom2 = message.GetUShort();
            term.Atom3 = message.GetUShort();
            term.kAngle = message.GetFloat();
            term.eqAngle = message.GetFloat();
            return term;
        }
        #endregion

        #region TorsionTerm
        public static Message AddTorsionTerm(this Message message, ForceField.TorsionTerm value) => Add(message, value);

        public static Message Add(this Message message, ForceField.TorsionTerm value)
        {
            message.AddUShort(value.Atom1);
            message.AddUShort(value.Atom2);
            message.AddUShort(value.Atom3);
            message.AddUShort(value.Atom4);
            message.AddFloat(value.vk);
            message.AddFloat(value.eqAngle);
            message.AddUShort(value.nn);
            return message;
        }

        public static ForceField.TorsionTerm GetTorsionTerm(this Message message)
        {
            var term = new ForceField.TorsionTerm();
            term.Atom1 = message.GetUShort();
            term.Atom2 = message.GetUShort();
            term.Atom3 = message.GetUShort();
            term.Atom4 = message.GetUShort();
            term.vk = message.GetFloat();
            term.eqAngle = message.GetFloat();
            term.nn = message.GetUShort();
            return term;
        }
        #endregion

        #region Guid
        public static Message AddGuid(this Message message, Guid value) => Add(message, value);

        public static Message Add(this Message message, Guid value)
        {
            message.AddBytes(value.ToByteArray());
            return message;
        }

        public static Guid GetGuid(this Message message)
        {
            var guid_bytes = message.GetBytes();
            return new Guid(guid_bytes);
        }
        #endregion

        #region Color
        public static Message AddColor(this Message message, Color value) => Add(message, value);

        public static Message Add(this Message message, Color value)
        {
            message.AddFloat(value.r);
            message.AddFloat(value.g);
            message.AddFloat(value.b);
            message.AddFloat(value.a);
            return message;
        }

        public static Color GetColor(this Message message)
        {
            var r = message.GetFloat();
            var g = message.GetFloat();
            var b  = message.GetFloat();
            var a = message.GetFloat();

            return new Color(r,g,b,a);
        }
        #endregion

        #region Pose
        /// <inheritdoc cref="Add(Message, Pose)"/>
        /// <remarks>Relying on the correct Add overload being chosen based on the parameter type can increase the odds of accidental type mismatches when retrieving data from a message. This method calls <see cref="Add(Message, Pose)"/> and simply provides an alternative type-explicit way to add a <see cref="Pose"/> to the message.</remarks>
        public static Message AddPose(this Message message, Pose value) => Add(message, value);

        /// <summary>Adds a <see cref="Pose"/> to the message.</summary>
        /// <param name="value">The <see cref="Pose"/> to add.</param>
        /// <returns>The message that the <see cref="Pose"/> was added to.</returns>
        public static Message Add(this Message message, Pose value)
        {
            message.AddVector3(value.position);
            message.AddQuaternion(value.rotation);
            return message;
        }

        /// <summary>Retrieves a <see cref="Pose"/> from the message.</summary>
        /// <returns>The <see cref="Pose"/> that was retrieved.</returns>
        public static Pose GetPose(this Message message)
        {
            return new Pose(message.GetVector3(), message.GetQuaternion());
        }
        #endregion
    }
}
