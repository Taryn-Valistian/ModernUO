/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2020 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: OutgoingPlayerPackets.cs                                        *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using Server.HuePickers;

namespace Server.Network
{
    public enum LRReason : byte
    {
        CannotLift,
        OutOfRange,
        OutOfSight,
        TryToSteal,
        AreHolding,
        Inspecific
    }

    public static class OutgoingPlayerPackets
    {
        public const int DragEffectPacketLength = 26;

        public static void SendStatLockInfo(this NetState ns, Mobile m)
        {
            if (ns == null || !ns.GetSendBuffer(out var buffer))
            {
                return;
            }

            var writer = new CircularBufferWriter(buffer);
            writer.Write((byte)0xBF); // Packet ID
            writer.Write((ushort)12);
            writer.Write((short)0x19);
            writer.Write((byte)2);
            writer.Write(m.Serial);
            writer.Write((byte)0);

            var lockBits = ((int)m.StrLock << 4) | ((int)m.DexLock << 2) | (int)m.IntLock;

            writer.Write((byte)lockBits);

            ns.Send(ref buffer, writer.Position);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SendChangeUpdateRange(this NetState ns, byte range)
        {
            ns?.Send(stackalloc byte[] { 0xC8, range });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SendDeathStatus(this NetState ns, bool dead)
        {
            ns?.Send(stackalloc byte[] { 0x2C, dead ? 0 : 2 });
        }

        public static void SendToggleSpecialAbility(this NetState ns, int abilityId, bool active)
        {
            if (ns == null || !ns.GetSendBuffer(out var buffer))
            {
                return;
            }

            var writer = new CircularBufferWriter(buffer);
            writer.Write((byte)0xBF); // Packet ID
            writer.Write((ushort)8);
            writer.Write((short)0x25);
            writer.Write((short)abilityId);
            writer.Write(active);

            ns.Send(ref buffer, writer.Position);
        }

        public static void SendDisplayProfile(this NetState ns, Serial m, string header, string body, string footer)
        {
            if (ns == null || !ns.GetSendBuffer(out var buffer))
            {
                return;
            }

            header ??= "";
            body ??= "";
            footer ??= "";

            var writer = new CircularBufferWriter(buffer);
            writer.Write((byte)0xB8); // Packet ID
            writer.Seek(2, SeekOrigin.Current);
            writer.Write(m);
            writer.WriteAsciiNull(header);
            writer.WriteBigUniNull(footer);
            writer.WriteBigUniNull(body);

            writer.WritePacketLength();
            ns.Send(ref buffer, writer.Position);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SendLiftReject(this NetState ns, LRReason reason)
        {
            ns?.Send(stackalloc byte[] { 0x27, (byte)reason });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SendLogoutAck(this NetState ns)
        {
            ns?.Send(stackalloc byte[] { 0xD1, 0x01 });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SendWeather(this NetState ns, byte type, byte density, byte temp)
        {
            ns?.Send(stackalloc byte[] { 0x65, type, density, temp });
        }

        public static void SendServerChange(this NetState ns, Point3D p, Map map)
        {
            if (ns == null || !ns.GetSendBuffer(out var buffer))
            {
                return;
            }

            var writer = new CircularBufferWriter(buffer);
            writer.Write((byte)0x76); // Packet ID
            writer.Write((short)p.X);
            writer.Write((short)p.Y);
            writer.Write((short)p.Z);
            writer.Write((byte)0);
            writer.Write((short)0);
            writer.Write((short)0);
            writer.Write((short)map.Width);
            writer.Write((short)map.Height);

            ns.Send(ref buffer, writer.Position);
        }

        public static void SendSkillsUpdate(this NetState ns, Skills skills)
        {
            if (ns == null || !ns.GetSendBuffer(out var buffer))
            {
                return;
            }

            var writer = new CircularBufferWriter(buffer);
            writer.Write((byte)0x3A); // Packet ID
            writer.Seek(2, SeekOrigin.Current);

            writer.Write((byte)0x02); // type: absolute, capped

            for (var i = 0; i < skills.Length; ++i)
            {
                var s = skills[i];

                var v = s.NonRacialValue;
                var uv = Math.Clamp((int)(v * 10), 0, 0xFFFF);

                writer.Write((ushort)(s.Info.SkillID + 1));
                writer.Write((ushort)uv);
                writer.Write((ushort)s.BaseFixedPoint);
                writer.Write((byte)s.Lock);
                writer.Write((ushort)s.CapFixedPoint);
            }

            writer.Write((short)0); // terminate

            writer.WritePacketLength();
            ns.Send(ref buffer, writer.Position);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SendSequence(this NetState ns, byte sequence)
        {
            ns?.Send(stackalloc byte[] { 0x7B, sequence });
        }

        public static void SendSkillChange(this NetState ns, Skill skill)
        {
            if (ns == null || !ns.GetSendBuffer(out var buffer))
            {
                return;
            }

            var writer = new CircularBufferWriter(buffer);
            writer.Write((byte)0x3A); // Packet ID
            writer.Write((ushort)13);

            var v = skill.NonRacialValue;
            var uv = Math.Clamp((int)(v * 10), 0, 0xFFFF);

            writer.Write((byte)0xDF); // type: delta, capped
            writer.Write((ushort)skill.Info.SkillID);
            writer.Write((ushort)uv);
            writer.Write((ushort)skill.BaseFixedPoint);
            writer.Write((byte)skill.Lock);
            writer.Write((ushort)skill.CapFixedPoint);

            ns.Send(ref buffer, writer.Position);
        }

        public static void SendLaunchBrowser(this NetState ns, string uri)
        {
            if (ns == null || !ns.GetSendBuffer(out var buffer))
            {
                return;
            }

            var writer = new CircularBufferWriter(buffer);
            writer.Write((byte)0xA5); // Packet ID
            writer.Seek(2, SeekOrigin.Current);
            writer.WriteAsciiNull(uri ?? "");

            writer.WritePacketLength();

            ns.Send(ref buffer, writer.Position);
        }

        public static void CreateDragEffect(
            Span<byte> buffer,
            Serial srcSerial, Point3D srcLocation,
            Serial trgSerial, Point3D trgLocation,
            int itemID, int hue, int amount
        )
        {
            var writer = new SpanWriter(buffer);
            writer.Write((byte)0x23); // Packet ID
            writer.Write((short)itemID);
            writer.Write((byte)0);
            writer.Write((short)hue);
            writer.Write((short)amount);
            writer.Write(srcSerial);
            writer.Write((short)srcLocation.X);
            writer.Write((short)srcLocation.Y);
            writer.Write((sbyte)srcLocation.Z);
            writer.Write(trgSerial);
            writer.Write((short)trgLocation.X);
            writer.Write((short)trgLocation.Y);
            writer.Write((sbyte)trgLocation.Z);
        }

        public static void SendDragEffect(
            this NetState ns,
            Serial srcSerial, Point3D srcLocation,
            Serial trgSerial, Point3D trgLocation,
            int itemID, int hue, int amount
        )
        {
            if (ns == null)
            {
                return;
            }

            Span<byte> span = stackalloc byte[DragEffectPacketLength];
            CreateDragEffect(span, srcSerial, srcLocation, trgSerial, trgLocation, itemID, hue, amount);
            ns.Send(span);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void SendSeasonChange(this NetState ns, byte season, bool playSound)
        {
            ns?.Send(stackalloc byte[]{ 0xBC, season, *(byte*)&playSound });
        }

        public static void SendDisplayPaperdoll(this NetState ns, Serial m, string title, bool warmode, bool canLift)
        {
            if (ns == null || !ns.GetSendBuffer(out var buffer))
            {
                return;
            }

            byte flags = 0x00;

            if (warmode)
            {
                flags |= 0x01;
            }

            if (canLift)
            {
                flags |= 0x02;
            }

            var writer = new CircularBufferWriter(buffer);
            writer.Write((byte)0x88); // Packet ID
            writer.Write(m);
            writer.WriteAscii(title, 60);
            writer.Write(flags);

            ns.Send(ref buffer, writer.Position);
        }

        public static void SendPlayMusic(this NetState ns, MusicName music)
        {
            if (ns == null || !ns.GetSendBuffer(out var buffer))
            {
                return;
            }

            var writer = new CircularBufferWriter(buffer);
            writer.Write((byte)0x6D); // Packet ID
            writer.Write((short)music);

            ns.Send(ref buffer, writer.Position);
        }

        public static void SendScrollMessage(this NetState ns, int type, int tip, string text)
        {
            if (ns == null || !ns.GetSendBuffer(out var buffer))
            {
                return;
            }

            text ??= "";

            var writer = new CircularBufferWriter(buffer);
            writer.Write((byte)0xA6); // Packet ID
            writer.Seek(2, SeekOrigin.Current);
            writer.Write((byte)type);
            writer.Write(tip);
            writer.Write((ushort)text.Length);
            writer.WriteAscii(text);

            writer.WritePacketLength();
            ns.Send(ref buffer, writer.Position);
        }

        // TODO: Use DateTime caching against core loop
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SendCurrentTime(this NetState ns) => ns.SendCurrentTime(DateTime.UtcNow);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SendCurrentTime(this NetState ns, DateTime date)
        {
            ns?.Send(stackalloc byte[] { 0x5B, (byte)date.Hour, (byte)date.Minute, (byte)date.Second });
        }

        public static void SendPathfindMessage(this NetState ns, Point3D p)
        {
            if (ns == null || !ns.GetSendBuffer(out var buffer))
            {
                return;
            }

            var writer = new CircularBufferWriter(buffer);
            writer.Write((byte)0x38); // Packet ID
            writer.Write((short)p.X);
            writer.Write((short)p.Y);
            writer.Write((short)p.Z);

            ns.Send(ref buffer, writer.Position);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SendPingAck(this NetState ns, byte ping)
        {
            ns?.Send(stackalloc byte[] { 0x73, ping });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SendClearWeaponAbility(this NetState ns)
        {
            ns?.Send(stackalloc byte[] { 0xBF, 0x00, 0x5, 0x00, 0x21 });
        }

        public static void SendDisplayHuePicker(this NetState ns, Serial huePickerSerial, int huePickerItemID)
        {
            if (ns == null || !ns.GetSendBuffer(out var buffer))
            {
                return;
            }

            var writer = new CircularBufferWriter(buffer);
            writer.Write((byte)0x95); // Packet ID
            writer.Write(huePickerSerial);
            writer.Write((short)0);
            writer.Write((short)huePickerItemID);

            ns.Send(ref buffer, writer.Position);
        }
    }
}
