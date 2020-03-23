﻿using LuteBot.Config;
using LuteBot.Core;
using LuteBot.TrackSelection;
using LuteBot.UI.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LuteBot.UI
{
    public partial class TrackSelectionForm : Form
    {
        TrackSelectionManager trackSelectionManager;
        MordhauOutDevice _mordhauOut;
        RustOutDevice _rustOut;

        // We need only one out device, might as well use rust, but take both for now cuz why not, feels unfair
        // Though they both get updated with the same values at the same time, for what we're doing
        public TrackSelectionForm(TrackSelectionManager trackSelectionManager, MordhauOutDevice mordhauOut, RustOutDevice rustOut)
        {
            _mordhauOut = mordhauOut;
            _rustOut = rustOut;
            this.trackSelectionManager = trackSelectionManager;
            trackSelectionManager.TrackChanged += new EventHandler(TrackChangedHandler);
            this.Load += TrackSelectionForm_Load;
            InitializeComponent();
            InitLists();
            trackSelectionManager.autoLoadProfile = AutoActivateCheckBox.Checked;
            typeof(Panel).InvokeMember("DoubleBuffered", BindingFlags.SetProperty
            | BindingFlags.Instance | BindingFlags.NonPublic, null,
            OffsetPanel, new object[] { true }); // Internet suggested this... 
        }

        private void TrackSelectionForm_Load(object sender, EventArgs e)
        {
            rowHeight = (OffsetPanel.Height - labelHeight * 2 - padding) / 2;
            columnWidth = (int)Math.Round(((OffsetPanel.Width - padding) / 10f) / 12);
            OffsetPanel.Paint += new PaintEventHandler(DrawGraphics);
            OffsetPanel.Resize += OffsetPanel_Resize;
            OffsetPanel.MouseDown += OffsetPanel_MouseDown;
            OffsetPanel.MouseUp += OffsetPanel_MouseUp;
            OffsetPanel.MouseLeave += OffsetPanel_MouseLeave;
            OffsetPanel.MouseCaptureChanged += OffsetPanel_MouseLeave; // These should do the same thing
            OffsetPanel.MouseMove += OffsetPanel_MouseMove;
            OffsetPanel.MouseEnter += OffsetPanel_MouseEnter;
        }

        // This mouse stuff is going to suck
        private Point dragStart;
        private bool dragging;
        private Rectangle draggableRect;
        private int startOffset;

        private double GetDistance(Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow((p2.X - p1.X), 2) + Math.Pow(p2.Y - p1.Y,2));
        }
        private void OffsetPanel_MouseEnter(object sender, EventArgs e) => OffsetPanel.Refresh();
        private void OffsetPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if(dragging)
            {
                // Set offset to (mousePost - dragStart)/columnWidth/12 (round to octaves...?)
                int multiplier = (e.Location.X < dragStart.X ? -1 : 1); // Preserve sign
                int oldOffset = trackSelectionManager.NoteOffset;
                trackSelectionManager.NoteOffset = startOffset + (int)Math.Round(((GetDistance(dragStart, e.Location)*multiplier / columnWidth)) / 12)*12;
                if (trackSelectionManager.NoteOffset != oldOffset)
                    Refresh();
            }

            //OffsetPanel.Refresh(); // See if this is laggy
            // Yeah a little

        }
        private void OffsetPanel_MouseLeave(object sender, EventArgs e)
        {
            dragging = false;
        }
        private void OffsetPanel_MouseUp(object sender, MouseEventArgs e)
        {
            dragging = false;
        }
        private void OffsetPanel_MouseDown(object sender, MouseEventArgs e)
        {
            // Check if their mouse is inside the draggableRect
            if (draggableRect.Contains(e.Location))
            {
                dragging = true;
                dragStart = e.Location;
                startOffset = trackSelectionManager.NoteOffset;
            }
        }

        private void OffsetPanel_Resize(object sender, EventArgs e)
        {
            OffsetPanel.Refresh();
        }

        private static readonly Color gridColor = Color.Gray;
        private static readonly Brush gridBrush = new SolidBrush(gridColor);
        private static readonly Pen gridPen = new Pen(gridBrush, 1);
        private static readonly Font gridFont = new Font(FontFamily.GenericSerif, 10f);
        private static readonly Font labelFont = new Font(FontFamily.GenericSerif, 10f, FontStyle.Bold);
        private static readonly Color gridHighlightColor = Color.DarkGoldenrod;
        private static readonly Brush gridHighlightBrush = new SolidBrush(gridHighlightColor);
        private static readonly Pen gridHighlightPen = new Pen(gridHighlightBrush, 2);
        private static readonly Color instrumentBarColor = Color.DarkSlateGray;
        private static readonly Brush instrumentBarBrush = new SolidBrush(instrumentBarColor);
        private static readonly Color draggableRectColor = Color.DarkGreen;
        private static readonly Brush draggableRectBrush = new SolidBrush(draggableRectColor);
        private static readonly Brush labelBrush = Brushes.Black;
        private static readonly Brush labelBgBrush = Brushes.Goldenrod;
        private static readonly Brush shadowBrush = Brushes.Black;

        int labelHeight = 20; // We have two labels, too, remember
        int padding = 6; // Seems to suck without this
        int xPad = 10;
        int rowHeight;
        int columnWidth;
        private void DrawGraphics(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            
            g.Clear(OffsetPanel.BackColor);
            // Draw the grid
            // We'll draw up to say 10 octaves
            // And we'll center the octaves around the center-ish of our current instrument's range

            

            
            // We need columnWidth to be a Note, not an octave


            int width = columnWidth * 10 * 12;
            int height = rowHeight * 2;
            // Let's go ahead and get the instrument info
            int lowest = ConfigManager.GetIntegerProperty(PropertyItem.LowestNoteId);
            int noteCount = ConfigManager.GetIntegerProperty(PropertyItem.AvaliableNoteCount);
            // Try to find the center
            var center = lowest + noteCount / 2;
            // Round down I guess
            center -= center % 12;
            int centerLine = center / 12; // This gives us the octave, like 0, 1, 2 usually
            // Now just take 5 - centerLine
            int centerOffset = 5 - centerLine;
            // and do the label as lineNum - that

            // We're going to iterate twice so the background grids are behind everything
            for (int x = xPad; x <= OffsetPanel.Width; x += columnWidth)
            {
                g.DrawLine(gridPen, x, 0, x, height);
            }

            // Draw the instrument bar first so the rest of the grid goes over it
            int rectStartX = (lowest * columnWidth) + xPad + (centerOffset*12*columnWidth);
            Rectangle instrumentRect = new Rectangle(rectStartX, 1, (noteCount * columnWidth), rowHeight - 1);
            g.FillRectangle(instrumentBarBrush, instrumentRect);

            // And same for the draggable one
            // So the width is the midi's note range, it starts at offset.  
            draggableRect = new Rectangle(xPad + trackSelectionManager.NoteOffset * columnWidth + _rustOut.LowMidiNoteId*columnWidth, rowHeight + 1, (_rustOut.HighMidiNoteId - _rustOut.LowMidiNoteId) * columnWidth, rowHeight - 1);
            g.FillRectangle(draggableRectBrush, draggableRect);

            
            // We use straight-up width because we want this to continue as far as it can
            for (int x = xPad; x <= OffsetPanel.Width; x += columnWidth)
            {
                // Draw vertical line
                
                // Put labels at the bottom
                int note = ((x - xPad) / columnWidth);

                // We want to add labels of what the draggableRect is at too (and lock it when it drags)
                // So we need to find our note relative to that... 
                // So just get _rustOut.LowMidiNoteId + trackSelectionManager.NoteOffset - this is the 'note' it starts at
                int midiLowest = _rustOut.LowMidiNoteId + trackSelectionManager.NoteOffset;
                int midiHighest = _rustOut.HighMidiNoteId + trackSelectionManager.NoteOffset;
                if(note >= midiLowest && note <= midiHighest)
                {
                    // We can consider showing labels...
                    // So then, (note - midiLowest) is how many notes out we are from midiLowest
                    // So midiLowest + (note - midiLowest) is our effective note here
                    int effectiveNote = _rustOut.LowMidiNoteId + (note - midiLowest);
                    if(effectiveNote % 12 == 0)
                    {
                        g.DrawString($"C{effectiveNote / 12}", gridFont, draggableRectBrush, x - xPad, height + padding + labelHeight);
                    }
                }


                if (note % 12 == 0)
                {
                    g.DrawString($"C{note / 12 - centerOffset}", gridFont, gridHighlightBrush, x - xPad, height + padding);
                    g.DrawLine(gridHighlightPen, x, 0, x, height);
                }

            }
            for (int y = 0; y <= height; y += rowHeight)
            {
                // Draw horizontal line
                g.DrawLine(gridHighlightPen, xPad, y, OffsetPanel.Width, y);
            }
            // Lastly, draw in white font above each of the squares to label them
            // instrumentRect and draggableRect
            // I'm too lazy to center the labels
            // But fine we need some rects behind them too.  
            int instrumentLabelWidth = 115;
            Rectangle instrumentLabelRect = new Rectangle(instrumentRect.X + instrumentRect.Width/2 - instrumentLabelWidth/2, instrumentRect.Y + rowHeight / 2 - 9, instrumentLabelWidth, 18);
            Rectangle instrumentLabelBgRect = new Rectangle(instrumentLabelRect.X+1, instrumentLabelRect.Y+1, instrumentLabelRect.Width+1, instrumentLabelRect.Height+1);
            g.FillRectangle(shadowBrush, instrumentLabelBgRect);
            g.FillRectangle(labelBgBrush, instrumentLabelRect);
            g.DrawString("Instrument Range", labelFont, labelBrush, instrumentLabelRect);
            // Guess at a width... this can be static
            int draggableLabelWidth = 168;
            Rectangle draggableLabelRect = new Rectangle(draggableRect.X + draggableRect.Width/2 - draggableLabelWidth/2, draggableRect.Y + rowHeight / 2 - 9, draggableLabelWidth, 18);
            Rectangle draggableLabelBgRect = new Rectangle(draggableLabelRect.X + 1, draggableLabelRect.Y + 1, draggableLabelRect.Width + 1, draggableLabelRect.Height + 1);
            g.FillRectangle(shadowBrush, draggableLabelBgRect);
            g.FillRectangle(labelBgBrush, draggableLabelRect);
            g.DrawString("Song Range (Click to Drag)", labelFont, labelBrush, draggableLabelRect);
        }

        private void InitLists()
        {
            TrackListBox.Items.Clear();
            ChannelsListBox.Items.Clear();
            foreach (MidiChannelItem channel in trackSelectionManager.MidiChannels)
            {
                ChannelsListBox.Items.Add(channel.Name, channel.Active);
            }
            foreach (TrackItem track in trackSelectionManager.MidiTracks)
            {
                TrackListBox.Items.Add(track.Name, track.Active);
            }
            Refresh();
            // This is a terrible thing to do, but, there's no easy way to hook the right events to make it wait properly
            // So after a timer, we're refreshing our OffsetPanel again
            Timer t = new Timer();
            t.Interval = 100;
            t.Tick += (object sender, EventArgs e) => Invoke((MethodInvoker) delegate { if(this.IsHandleCreated) OffsetPanel.Refresh();  t.Dispose(); });
            t.Start();
        }

        private void TrackChangedHandler(object sender, EventArgs e)
        {
            InitLists();
        }

        private void TrackSelectionForm_Closing(object sender, FormClosingEventArgs e)
        {
            WindowPositionUtils.UpdateBounds(PropertyItem.TrackSelectionPos, new Point() { X = Left, Y = Top });
            ConfigManager.SaveConfig();
        }

        private void SelectAllChannelsTextBox_CheckedChanged(object sender, EventArgs e)
        {
            trackSelectionManager.ActivateAllChannels = SelectAllChannelsCheckBox.Checked;
            ChannelsListBox.Enabled = !SelectAllChannelsCheckBox.Checked;
        }

        private void ChannelListBox_ItemChecked(object sender, ItemCheckEventArgs e)
        {
            trackSelectionManager.ToggleChannelActivation(!(e.CurrentValue == CheckState.Checked), e.Index);
        }

        private void SongProfileSaveButton_Click(object sender, EventArgs e)
        {
            if (trackSelectionManager.FileName != null)
            {
                trackSelectionManager.SaveTrackManager();
            }
        }

        private void LoadProfileButton_Click(object sender, EventArgs e)
        {
            if (trackSelectionManager.FileName != null)
            {
                trackSelectionManager.LoadTrackManager();
                InitLists();
            }
        }

        private void AutoActivateCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            trackSelectionManager.autoLoadProfile = AutoActivateCheckBox.Checked;
        }

        private void SelectAllTracksCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            trackSelectionManager.ActivateAllChannels = SelectAllTracksCheckBox.Checked;
            TrackListBox.Enabled = !SelectAllTracksCheckBox.Checked;
        }

        private void TrackListBox_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            trackSelectionManager.ToggleTrackActivation(!(e.CurrentValue == CheckState.Checked), e.Index);
        }
    }
}
