using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets {
	public class WorkshopBundleLoader {
		string _directory;

		public static void Load( string directory ) {
			WorkshopBundleLoader loader = new Assets.WorkshopBundleLoader();
			loader._directory = Path.GetFullPath( directory );
			loader.Load();
		}

		private void Load() {
			string xmlFilePath = Path.Combine( _directory, "bundle.xml" );

			ReadWholeFile( xmlFilePath, (error, bundleBinary) => {
			} );
		}

		private void ReadWholeFile( string filePath, Action<Exception, byte[]> callback ) {
			FileStream stream;

			try {
				stream = new FileStream( filePath, FileMode.Open, FileAccess.Read, FileShare.Read );
			}
			catch( Exception ex ) {
				callback( ex, null );
				return;
			}

			MemoryStream memoryStream = new MemoryStream( (int) stream.Length );

			Action read = () => {
				byte[] buffer = new byte[4096];

				stream.BeginRead( buffer, 0, buffer.Length, asyncResult => {
					int readCount;

					try {
						readCount = stream.EndRead( asyncResult );
					}
					catch( Exception ex ) {
						callback( ex, null );
						return;
					}

					if( readCount != 0 ) {
						memoryStream.Write( buffer, 0, readCount );
					}
					else {
						stream.Dispose();
						callback( null, memoryStream.ToArray() );
						return;
					}
				}, null );
			};

			read();
		}

		public bool Done {
			get {
				throw new NotImplementedException();
			}
		}

		public int Progress {
			get {
				throw new NotImplementedException();
			}
		}

		public int ProgressTotal {
			get {
				throw new NotImplementedException();
			}
		}

		public WorkshopBundle Result {
			get {
				throw new NotImplementedException();
			}
		}
	}

	public class WorkshopBundle {
		ulong _publishedFileId;
		List<WorkshopItem> _items;

	}

	public class WorkshopItem {
		string _name;
		Guid _guid;

		public WorkshopItem( Guid guid ) {
			_guid = guid;
		}

		public Guid Guid {
			get {
				return _guid;
			}
		}

		public string Name {
			get {
				return _name;
			}
			set {
				_name = value;
			}
		}
	}

	public class WorkshopDecorativeObject : WorkshopItem {
		ObjectModel _model, _collisionModel;
		//MaterialData _material;
	}

	/*public class WorkshopToken : WorkshopItem {

	}*/

	public class WorkshopTexture : WorkshopItem {
		byte[] _textureFileData;
		Texture2D _texture;

		public WorkshopTexture( Guid guid, byte[] fileData ) : base( name ) {
			if( fileData == null )
				throw new ArgumentNullException( "fileData" );

			_textureFileData = fileData;
		}

		public Texture2D Texture {
			get {
				if( _texture == null ) {
					_texture = new Texture2D( 2, 2 );
					_texture.LoadImage( _textureFileData, false );
				}

				return _texture;
			}
		}
	}

/*	public class WorkshopTile : WorkshopItem {
		ObjectModel _model;
		int _height, _width;
	}

	public class WorkshopTileSet : WorkshopItem {
	}*/

	public class ObjectModel {
		string _objText;
		Mesh _mesh;

		public ObjectModel( string objText ) {
			if( objText == null )
				throw new ArgumentNullException( "objText" );

			_objText = objText;
		}

		public Mesh Mesh {
			get {
				if( _mesh == null ) {
					_mesh = ObjImporter.ImportFile( _objText );
				}

				return _mesh;
			}
		}
	}

/*	public class MaterialData {
		//Shader ID.  Index to a fixed list of shaders.
		public int shaderID;

		//Diffuse
		public SVector4 diffuseColor;
		public string diffuseTexture;

		//Specularity
		public SVector4 specularColor;
		public string specularTexture;
		public float specularHardness;

		//Emmissive
		public SVector4 emissiveColor;
		public string emmisiveTexture;

		//Normal
		public string normalTexture;

		//Ambient Occlusion
		public string AOTexture;

		//Creates and returns a new MaterialData from an existing Material
		public static MaterialData CreateMaterialData( Material material ) {
			MaterialData md = new MaterialData();

			//TODO: Create shader reference list.
			md.shaderID = 0;

			md.diffuseColor = material.color;
		}
	}*/
}
