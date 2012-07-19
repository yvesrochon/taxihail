using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace apcurium.MK.Booking.Mobile.Client.Adapters
{
    public class CustomOrderListAdapter : BaseAdapter
    {
        private Activity _context;

        public IDictionary<string, OrderListAdapter> Sections { get; set; }
        public ArrayAdapter<string> Headers { get; set; }
        public static int TYPE_SECTION_HEADER = 0;

        public static string ITEM_TITLE = "TITLE";
        public static string ITEM_SUBTITLE = "SUBTITLE";
        public static string ITEM_ID = "ID";

        public CustomOrderListAdapter(Activity context)
            : base()
        {
            _context = context;
            Headers = new ArrayAdapter<string>(context, Resource.Layout.ListHeader);
            Sections = new Dictionary<string, OrderListAdapter>();
        }
        public void AddSection(string section, OrderListAdapter adapter)
        {
            Headers.Add(section);
            Sections.Add(new KeyValuePair<string, OrderListAdapter>(section, adapter));
        }

        public override int Count
        {
            get
            {
                int total = 0;
                foreach (var adapter in this.Sections.Values)
                {
                    total += adapter.Count + 1;
                }
                return total;
            }
        }
        public override int ViewTypeCount
        {
            get
            {
                int total = 1;
                foreach (var adapter in this.Sections.Values)
                {
                    total += adapter.ViewTypeCount;
                }


                return total;
            }
        }

        public override Java.Lang.Object GetItem(int position)
        {
            foreach (var section in this.Sections.Keys)
            {
                OrderListAdapter adapter = Sections[section];
                int size = adapter.Count + 1;
                if (position == 0)
                {
                    return section;
                }
                if (position < size)
                {
                    return adapter.GetItem(position - 1);
                }
                position -= size;

            }

            return null;
        }

        public override int GetItemViewType(int position)
        {
            int type = 1;
            foreach (var section in this.Sections.Keys)
            {
                OrderListAdapter adapter = this.Sections[section];
                int size = adapter.Count + 1;
                if (position == 0)
                {
                    return TYPE_SECTION_HEADER;
                }
                if (position < size)
                {
                    return type + adapter.GetItemViewType(position - 1);
                }
                position -= size;
                type += adapter.ViewTypeCount;
            }
            return -1;
        }
        public override bool AreAllItemsEnabled()
        {
            return false;
        }

        public override long GetItemId(int position)
        {
            return position;
        }
        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            int sectionNum = 0;
            foreach (var section in this.Sections.Keys)
            {
                OrderListAdapter adapter = this.Sections[section];
                int size = adapter.Count + 1;

                // check if position inside this section
                if (position == 0)
                {
                    return Headers.GetView(sectionNum, convertView, parent);
                }
                if (position < size)
                {
                    /*var item = adapter.GetItem(position - 1).Cast<IDictionary<string, object>>();                    
                    
                    var inflater = (LayoutInflater)_context.GetSystemService(Android.Content.Context.LayoutInflaterService);
                    
                    var v = inflater.Inflate(Resource.Layout.SimpleListItem, parent, false );

                    if ( item != null )
                    {
                        v.FindViewById<TextView>(Resource.Id.ListComplexTitle).Text = item[ITEM_TITLE].ToString();
                        v.FindViewById<TextView>(Resource.Id.ListComplexSubtitle).Text = item[ITEM_SUBTITLE].ToString();
                    }*/



                    return adapter.GetView(position - 1, convertView, parent);
                    //_headerView = inflater.Inflate(info.HeaderLayoutResourceId, headerContainer, false);


                    //return new TextView(_context) { Text = "AAA" };
                    //var view = adapter.GetView(position - 1, convertView, parent);
                    //return view;

                }

                // otherwise jump into next section
                position -= size;
                sectionNum++;
            }
            return null;
        }
    }
}
