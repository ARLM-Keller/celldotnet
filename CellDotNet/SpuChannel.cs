// 
// Copyright (C) 2007 Klaus Hansen and Rasmus Halland
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;

namespace CellDotNet
{
	/// <summary>
	/// SPU read channels as defined in CBEA chapter 9.
	/// </summary>
	enum SpuReadChannel
	{
		/// <summary>
		/// SPU Read Event Status Channel.
		/// Read event status (with mask applied).
		/// <para>Read blocking</para>
		/// </summary>
		SPU_RdEventStat = 0,

		/// <summary>
		/// SPU Signal Notification 1 Channel.
		/// <para>Read blocking</para>
		/// </summary>
		SPU_RdSigNotify1 = 0x3,

		/// <summary>
		/// SPU Signal Notification 2 Channel.
		/// <para>Read blocking</para>
		/// </summary>
		SPU_RdSigNotify2 = 0x4,

		/// <summary>
		/// SPU Read Decrementer Channel.
		/// <para>Read</para>
		/// </summary>
		SPU_RdDec = 0x8,

		/// <summary>
		/// SPU Read Event Mask Channel.
		/// <para>Read</para>
		/// </summary>
		SPU_RdEventMask = 0xb,

		/// <summary>
		/// MFC Read Tag-Group Query Mask Channel.
		/// <para>Read</para>
		/// </summary>
		MFC_RdTagMask = 0xc,

		/// <summary>
		/// SPU Read Machine Status Channel.
		/// <para>Read</para>
		/// </summary>
		SPU_RdMachStat = 0xd,

		/// <summary>
		/// SPU Read State Save-and-Restore Channel.
		/// <para>Read</para>
		/// </summary>
		SPU_RdSRR0 = 0xf,

		/// <summary>
		/// MFC Read Tag-Group Status Channel.
		/// Read tag status (with mask applied).
		/// <para>Read blocking</para>
		/// </summary>
		MFC_RdTagStat = 0x18,

		/// <summary>
		/// MFC Read List Stall-and-Notify Tag Status Channel.
		/// Read MFC list stall-and-notify status.
		/// <para>Read blocking</para>
		/// </summary>
		MFC_RdListStallStat = 0x19,

		/// <summary>
		/// MFC Read Atomic Command Status Channel.
		/// Read atomic command status.
		/// <para>Read blocking</para>
		/// </summary>
		MFC_RdAtomicStat = 0x1b,

		/// <summary>
		/// SPU Read Inbound Mailbox Channel.
		/// Read inbound SPU mailbox contents.
		/// <para>Read blocking</para>
		/// </summary>
		SPU_RdInMbox = 0x1d,
	}

	/// <summary>
	/// SPU write channels as defined in CBEA chapter 9.
	/// </summary>
	enum SpuWriteChannel
	{
		/// <summary>
		/// SPU Write Event Mask Channel.
		/// Write event-status mask.
		/// <para>Write</para>
		/// </summary>
		SPU_WrEventMask = 0x1,

		/// <summary>
		/// SPU Write Event Acknowledgment Channel.
		/// Write end-of-event processing.
		/// <para>Write</para>
		/// </summary>
		SPU_WrEventAck = 0x2,

		/// <summary>
		/// SPU Write Decrementer Channel.
		/// <para>Write</para>
		/// </summary>
		SPU_WrDec = 0x7,

		/// <summary>
		/// MFC Write Multisource Synchronization Request Channel.
		/// <para>Write blocking</para>
		/// </summary>
		MFC_WrMSSyncReq = 0x9,

		/// <summary>
		/// SPU Write State Save-and-Restore Channel.
		/// <para>Write</para>
		/// </summary>
		SPU_WrSRR0 = 0xe,

		/// <summary>
		/// MFC Local Storage Address Channel.
		/// Write local storage address command parameter.
		/// <para>Write</para>
		/// </summary>
		MFC_LSA = 0x10,

		/// <summary>
		/// MFC Effective Address High Channel.
		/// Write high-order MFC SPU effective-address command parameter.
		/// <para>Write</para>
		/// </summary>
		MFC_EAH = 0x11,

		/// <summary>
		/// MFC Effective Address Low or List Address Channel.
		/// Write low-order MFC SPU effective-address command parameter.
		/// <para>Write</para>
		/// </summary>
		MFC_EAL = 0x12,

		/// <summary>
		/// MFC Transfer Size or List Size Channel.
		/// Write MFC SPU transfer size command parameter.
		/// <para>Write</para>
		/// </summary>
		MFC_Size = 0x13,

		/// <summary>
		/// MFC Command Tag Identification Channel.
		/// Write MFC SPU tag identifier command parameter.
		/// <para>Write</para>
		/// </summary>
		MFC_TagID = 0x14,

		/// <summary>
		/// <para>
		/// MFC Command Opcode Channel.
		/// Write and enqueue MFC SPU command with associated class ID.
		/// </para>
		/// <para>
		/// MFC Class ID Channel.
		/// Write and enqueue MFC SPU command with associated command opcode.
		/// </para>
		/// <para>Write blocking</para>
		/// </summary>
		MFC_CmdAndClassID = 0x15,

		/// <summary>
		/// MFC Write Tag-Group Query Mask Channel.
		/// Write tag mask.
		/// <para>Write</para>
		/// </summary>
		MFC_WrTagMask = 0x16,

		/// <summary>
		/// MFC Write Tag Status Update Request Channel.
		/// Write request for conditional or unconditional tag status update.
		/// <para>Write blocking</para>
		/// </summary>
		MFC_WrTagUpdate = 0x17,

		/// <summary>
		/// MFC Write List Stall-and-Notify Tag Acknowledgment Channel.
		/// Write MFC list stall-and-notify acknowledgment.
		/// <para>Write</para>
		/// </summary>
		MFC_WrListStallAck = 0x1a,

		/// <summary>
		/// SPU Write Outbound Mailbox Channel.
		/// Write outbound SPU mailbox contents.
		/// <para>Write blocking</para>
		/// </summary>
		SPU_WrOutMbox = 0x1c,

		/// <summary>
		/// SPU Write Outbound Interrupt Mailbox Channel.
		/// Write SPU outbound interrupt mailbox contents.
		/// <para>Write blocking</para>
		/// </summary>
		SPU_WrOutIntrMbox = 0x1e,
	}
}
