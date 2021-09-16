using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FavesConverter
{
    public enum FavoriteType
    {
        Invalid,
        Stack,
        RoughlyEnoughItems_EntryStack
    }

    public class Favorite
    {
        public string str;
        public string value;
        public string newvalue;
        public FavoriteType type;

        public Favorite( string s, FavoriteType type )
        {
            this.str = s;
            this.type = type;
        }

        public void ExtractData()
        {
            int i = 0;

            if( type == FavoriteType.Stack )
            {
                i++; // skip {

                Program.SkipWhiteSpaces( str, ref i );

                i += 16; // skip type

                Program.SkipWhiteSpaces( str, ref i );

                i += 8;
                int start = i;

                this.value = str.Substring( start, str.Length - start - 4 );
            }
            if( type == FavoriteType.RoughlyEnoughItems_EntryStack )
            {
                i++; // skip {

                Program.SkipWhiteSpaces( str, ref i );

                i += 41; // skip type

                Program.SkipWhiteSpaces( str, ref i );

                i += 8;
                int start = i;

                this.value = str.Substring( start, str.Length - start - 3 );
            }
        }

        public void GetNewValue()
        {
            if( this.type == FavoriteType.Stack )
            {
                newvalue = value.Substring( 1, value.Length - 2 );
            }

            if( this.type == FavoriteType.RoughlyEnoughItems_EntryStack )
            {
                string val = value.Substring( 1, value.Length - 2 );

                // strip from all whitespaces (not inside "")

                StringBuilder sb = new StringBuilder();

                bool insideSingle = false;
                bool insideDouble = false;

                for( int i = 0; i < val.Length; i++ )
                {
                    if( !insideDouble )
                    {
                        if( val[i] == '\'' )
                        {
                            if( i > 0 && val[i - 1] != '\\' ) // guard against checking outside the string.
                            {
                                insideSingle = !insideSingle;
                            }
                        }
                    }
                    if( !insideSingle )
                    {
                        if( val[i] == '"' ) //|| s[i] == '\'' )
                        {
                            if( i > 0 && val[i - 1] != '\\' ) // guard against checking outside the string.
                            {
                                insideDouble = !insideDouble;
                            }
                        }
                    }

                    /*if( val[i] == '"' || val[i] == '\'' )
                    {
                        if( i > 0 && val[i - 1] != '\\' ) // don't switch when moving over an escaped string.
                        {
                            inside = !inside;
                        }
                    }*/

                    if( (!insideSingle && !insideDouble) && char.IsWhiteSpace( val[i] ) )
                    {
                        continue;
                    }
                    sb.Append( val[i] );
                }

                val = sb.ToString();
                //newvalue = val;


                //inside = false;
                //insideSingle = false;
                //insideDouble = false;

                List<int> indicesToStrip = new List<int>();
                int namestart = -1;
                int nameend = -1;
                for( int i = 0; i < val.Length; i++ )
                {
                    if( val[i] == '"' && (i == 0 || (i > 0 && val[i - 1] != '\\')) ) // if we encounter a ", and that " is not escaped
                    {
                        if( i < val.Length - 1 && val[i + 1] == ':' )
                        {
                            nameend = i;
                            indicesToStrip.Add( namestart );
                            indicesToStrip.Add( nameend );

                            namestart = -1;
                            nameend = -1;
                        }
                        else
                        {
                            namestart = i;
                        }
                    }
                }

                sb = new StringBuilder();
                int index = 0;
                for( int i = 0; i < val.Length; i++ )
                {
                    if( index < indicesToStrip.Count && i == indicesToStrip[index] )
                    {
                        index++;
                    }
                    else
                    {
                        sb.Append( val[i] );
                    }
                }

                val = sb.ToString();
               // newvalue = val;

                //inside = false;
                sb = new StringBuilder();
                for( int i = 0; i < val.Length; i++ )
                {
                    
                    if( val[i] == '"' || val[i] == '\\' )
                    {
                        sb.Append( '\\' );
                    }
                    sb.Append( val[i] );
                }

                val = sb.ToString();

                newvalue = val;
            }

            newvalue = "\"{data:{" + newvalue + ",type:\\\"minecraft:item\\\"},type:\\\"roughlyenoughitems:entry_stack\\\"}\"";
        }
    }


    public class Program
    {
        public static void SkipWhiteSpaces( string s, ref int i )
        {
            while( (i < s.Length - 1) && char.IsWhiteSpace( s[i] ) )
            {
                i++;
            }
        }

        public static FavoriteType ReadType( string s, ref int i )
        {

            // skip 3
            //i += 4;
            // read the type
            char si = s[i];
            if( s[i] == 'r' )
            {
                for( ; i < s.Length; i++ )
                {
                    if( s[i] == '"' )
                    {
                        i++;
                        break;
                    }
                }
                // "roughlyenoughitems:entry_stack"
                return FavoriteType.RoughlyEnoughItems_EntryStack;
            }
            if( s[i] == 's' )
            {
                for( ; i < s.Length; i++ )
                {
                    if( s[i] == '"' )
                    {
                        i++;
                        break;
                    }
                }
                // "stack"
                return FavoriteType.Stack;
            }

            throw new Exception( "Didn't encounter a valid type" );
        }

        public static Favorite GetFavourite( string s, ref int pos )
        {
            // returns the favourite entry, moves the pos to the front of the next one.

            int nestLevel = 0;
            bool wentAboveNest0 = false;
            bool isInsideDoubleString = false;
            bool isInsideSingleString = false;
            FavoriteType type = FavoriteType.Invalid;
            int start = pos;
            int i = pos;


            i += 14;

            char spos = s[i];

            type = ReadType( s, ref i );

            i = pos;

            for( ; i < s.Length; i++ )
            {
                // check if the current head position is inside a string tag, if so, don't count the nest level, because {} can exist inside string tags.
                if( !isInsideDoubleString )
                {
                    if( s[i] == '\'' )
                    {
                        if( i == 0 || (i > 0 && s[i - 1] != '\\') ) // guard against checking outside the string.
                        {
                            isInsideSingleString = !isInsideSingleString;
                        }
                    }
                }
                if( !isInsideSingleString )
                {
                    if( s[i] == '"' ) //|| s[i] == '\'' )
                    {
                        if( i == 0 || (i > 0 && s[i - 1] != '\\') ) // guard against checking outside the string.
                        {
                            isInsideDoubleString = !isInsideDoubleString;
                        }
                    }
                }
                if( s[i] == '{' && (!isInsideSingleString && !isInsideDoubleString) ) // this style is needed to extract data from entrystack
                {
                    nestLevel++;
                    wentAboveNest0 = true; // we found a favourite entry (starts with '{')
                }
                else if( s[i] == '}' && (!isInsideSingleString && !isInsideDoubleString) )
                {
                    nestLevel--;
                }

                // if we are at nest level 0 - check if we went above nest level 0 (is we did, that means we found a favourite entry)
                if( nestLevel == 0 )
                {
                    if( wentAboveNest0 )
                    {
                        i++;

                        pos = i; // skip one more

                        string favestring = s.Substring( start, pos - start );

                        // we are at the end of the fave entry
                        if( i < s.Length - 1 && s[i+1] == ',' )
                        {
                            i++;
                        }

                        i++;

                        SkipWhiteSpaces( s, ref i );

                        pos = i; // skip one more


                        return new Favorite( favestring, type );
                    }
                }
            }
            throw new Exception( "Didn't find entry" );
        }

        static void Main( string[] args )
        {
            string file = File.ReadAllText( AppDomain.CurrentDomain.BaseDirectory + Path.DirectorySeparatorChar + "inputfavorites2.txt" );
            List<Favorite> entries = new List<Favorite>();
            int pos = 0;
            while( pos < file.Length )
            {
                Favorite entry = GetFavourite( file, ref pos );
                entry.ExtractData();
                entry.GetNewValue();
                entries.Add( entry );
                Console.WriteLine( "" );
                Console.WriteLine( "||" + entry.newvalue + "||" );
                Console.WriteLine( "" );
            }

            StringBuilder sb = new StringBuilder();
            for( int i = 0; i < entries.Count - 1; i++ )
            {
                sb.Append( entries[i].newvalue );
                sb.Append( "," );
                sb.Append( "\n" );
            }
            sb.Append( entries[entries.Count-1].newvalue );

            File.WriteAllText( AppDomain.CurrentDomain.BaseDirectory + Path.DirectorySeparatorChar + "output.txt", sb.ToString() );

            Console.WriteLine( "Press any key to continue..." );
            Console.ReadKey();
        }
    }
}
