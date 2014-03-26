using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.IO;
using System.Xml.Serialization;
using System.Drawing;

namespace Gestures
{
    /*NOTE: Simply handles database while training*/
    public class Database
    {
        public BindingList<string> Classes { get; private set; }
        public BindingList<Sequence> Samples { get; private set; }


        public Database()
        {
            Classes = new BindingList<string>();
            Samples = new BindingList<Sequence>(); //Sequence class modified for 3D gesture sequence storage
        }

        public void Save(Stream stream)
        {
            var serializer = new XmlSerializer(typeof(BindingList<Sequence>));
            serializer.Serialize(stream, Samples);
        }

        public void Load(Stream stream)
        {
            var serializer = new XmlSerializer(typeof(BindingList<Sequence>));
            var samples = (BindingList<Sequence>)serializer.Deserialize(stream);

            Classes.Clear();
            foreach (string label in samples.First().Classes)
                Classes.Add(label);

            Samples.Clear();
            foreach (Sequence sample in samples)
            {
                sample.Classes = Classes;
                Samples.Add(sample);
            }
        }

        //adds the GestureData obj and classLabel to database
        public Sequence Add(GestureData[] sequence, string classLabel)
        {
            if (sequence == null || sequence.Length == 0)
                return null;

            if (!Classes.Contains(classLabel))
                Classes.Add(classLabel);

            int classIndex = Classes.IndexOf(classLabel);

            //create a new obj of Sequence class, to add to the Samples<Sequence> list
            Sequence sample = new Sequence()
            {
                Classes = Classes, //all the classes are stored in a list form
                SourcePath = sequence, //the GestureData obj is added
                Output = classIndex //the class label (int form)
            };

            Samples.Add(sample);

            return sample;
        }

        public void Clear()
        {
            Classes.Clear();
            Samples.Clear();
        }

        public int SamplesPerClass()
        {
            int min = 0;
            foreach (string label in Classes)
            {
                int c = Samples.Count(p => p.OutputName == label);

                if (min == 0) 
                    min = c;

                else if (c < min)
                    min = c;
            }

            return min;
        }
    }
}
