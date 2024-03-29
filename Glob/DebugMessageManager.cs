﻿using System;
using OpenTK.Graphics.OpenGL;

namespace Glob
{
	// TODO: rewrite to non-static?
	/// <summary>
	/// Used to get realtime text output about OpenGL errors and warnings.
	/// </summary>
	public class DebugMessageManager
	{
		public bool Enabled { get; private set; }

		public DebugMessageManager(bool enabled)
		{
			Enabled = enabled;
		}

		/// <summary>
		/// Pushes debug group marker. Can be used in a using statement to pop the marker automatically (by disposing the returned object)
		/// </summary>
		/// <param name="marker">Debug marker label</param>
		/// <returns>Object that pops the group marker upon being disposed</returns>
		public IDisposable PushGroupMarker(string marker)
		{
			if(!Enabled)
				return new EmptyDisposable();

			GL.PushDebugGroup(DebugSourceExternal.DebugSourceApplication, GetMessageID(), -1, marker);

			return new PopDebugMarkerClass(this);
		}

		public void PopGroupMarker()
		{
			GL.PopDebugGroup();
		}

		int _giveMessageID = 1;

		int GetMessageID()
		{
			return _giveMessageID++;
		}

		static DebugProc _callback;

		public static void SetupDebugCallback(ITextOutputGlob textOutput)
		{
			_debugOutput = textOutput;
			GL.Enable(EnableCap.DebugOutput);
			GL.Enable(EnableCap.DebugOutputSynchronous);

			_callback = DebugMessageCallback;
			GL.DebugMessageCallback(_callback, IntPtr.Zero);
			_maxDebugMessageLength = GL.GetInteger((GetPName)GL_MAX_DEBUG_MESSAGE_LENGTH);
			GL.DebugMessageControl(DebugSourceControl.DontCare, DebugTypeControl.DontCare, DebugSeverityControl.DontCare, 0, new int[1], true);
			GL.DebugMessageControl(DebugSourceControl.DontCare, DebugTypeControl.DebugTypePushGroup, DebugSeverityControl.DontCare, 0, new int[1], false);
			GL.DebugMessageControl(DebugSourceControl.DontCare, DebugTypeControl.DebugTypePopGroup, DebugSeverityControl.DontCare, 0, new int[1], false);
			//GL.DebugMessageControl(DebugSourceControl.DontCare, DebugTypeControl.DontCare, DebugSeverityControl.DontCare, 2, new int[2]
			//{
			//	131185,
			//	131186,
			//}, false);
		}

		public static void GetDebugMessageLog()
		{
			int count = 100;
			DebugSource[] sources = new DebugSource[count];
			DebugType[] types = new DebugType[count];
			int[] ids = new int[count];
			DebugSeverity[] severities = new DebugSeverity[count];
			int[] lengths = new int[count];
			string log;
			int actualCount = GL.GetDebugMessageLog(count, _maxDebugMessageLength * count, sources, types, ids, severities, lengths, out log);
			var messages = log.Split('\0');

			for(int i = 0; i < actualCount; i++)
			{
				ProcessMessage(sources[i], types[i], ids[i], severities[i], messages[i]);
			}
		}
		
		static void DebugMessageCallback(DebugSource source, DebugType type, int id, DebugSeverity severity, int length, IntPtr message, IntPtr userParam)
		{
			string messageString = System.Runtime.InteropServices.Marshal.PtrToStringAnsi(message, length);
			ProcessMessage(source, type, id, severity, messageString);
		}

		static void ProcessMessage(DebugSource source, DebugType type, int id, DebugSeverity severity, string message)
		{
			if(type == DebugType.DebugTypePushGroup || type == DebugType.DebugTypePopGroup)
				return;
			string msg = message + " (source: " + source.ToString() + "; type: " + type.ToString() + "; id: " + id.ToString() + "; severity: " + severity.ToString() + ")";

			OutputTypeGlob outputType = OutputTypeGlob.LogOnly;

			if(severity == DebugSeverity.DebugSeverityMedium || severity == DebugSeverity.DebugSeverityHigh)
				outputType = OutputTypeGlob.PerformanceWarning;
			if(id == 131185 || id == 131186 || id == 131154) // Buffer memory info, pixel path performance (warning generated by blitFramebuffers)
				return;

			_debugOutput.Print(outputType, msg);
			_debugOutput.Print(OutputTypeGlob.LogOnly, Environment.StackTrace);
		}

		const int GL_MAX_DEBUG_MESSAGE_LENGTH = 0x9143;
		const int GL_DEBUG_LOGGED_MESSAGES = 0x9145;
		static ITextOutputGlob _debugOutput;
		static int _maxDebugMessageLength;

		class PopDebugMarkerClass : IDisposable
		{
			DebugMessageManager _debugMessageManager;

			public PopDebugMarkerClass(DebugMessageManager manager)
			{
				_debugMessageManager = manager;
			}

			public void Dispose()
			{
				if(!_debugMessageManager.Enabled)
					return;

				_debugMessageManager.PopGroupMarker();
			}
		}
	}
}
