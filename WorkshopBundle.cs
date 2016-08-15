using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets {
	class WorkshopBundleLoader {
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

		public static void ReadWholeFile( string filePath, Action<Exception, byte[]> callback ) {
			FileStream stream;
			Debug.Log("here1");

			try {
				stream = new FileStream( filePath, FileMode.Open, FileAccess.Read, FileShare.Read );
			}
			catch( Exception ex ) {
				callback( ex, null );
				return;
			}

			MemoryStream memoryStream = new MemoryStream( (int) stream.Length );

			Action read = () => { };

			read = () => {
				byte[] buffer = new byte[4096];
				//Debug.Log("here");

				stream.BeginRead( buffer, 0, buffer.Length, asyncResult => {
					int readCount;

					try {
						readCount = stream.EndRead( asyncResult );
					}
					catch( Exception ex ) {
						callback( ex, null );
						return;
					}
					//Debug.Log(readCount);

					if( readCount != 0 ) {
						memoryStream.Write( buffer, 0, readCount );
						read();
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
		List<WorkshopItem> _items = new List<WorkshopItem>();
		const string __models = "Models", __textures = "Textures";

		public WorkshopTexture GetTexture( string name ) {
			return _items.OfType<WorkshopTexture>().FirstOrDefault( t => t.Name == name );
		}

		public WorkshopModel GetModel( string name ) {
			return _items.OfType<WorkshopModel>().FirstOrDefault( t => t.Name == name );
		}

		public List<WorkshopItem> Items
			{ get { return _items; } }

		public IEnumerator LoadDirectory( string directory ) {
			directory = Path.GetFullPath( directory );

			// Collect the lists of models and textures currently in the directory
			string modelsPath = Path.Combine( directory, __models );
			HashSet<string> modelPaths;

			if( Directory.Exists( modelsPath ) ) {
				modelPaths = new HashSet<string>( Directory.GetFiles( modelsPath ).Select( p => Path.Combine( __models, Path.GetFileName( p ) ) ) );
			}
			else {
				modelPaths = new HashSet<string>();
			}

			string texturesPath = Path.Combine( directory, __textures );
			HashSet<string> texturePaths;

			if( Directory.Exists( texturesPath ) ) {
				texturePaths = new HashSet<string>( Directory.GetFiles( texturesPath ).Select( p => Path.Combine( __textures, Path.GetFileName( p ) ) ) );
				foreach(string path in texturePaths)
					Debug.Log(path);
			}
			else {
				texturePaths = new HashSet<string>();
			}

			// Go through all of our existing models/textures, reload them, and remove them from the new lists
			foreach( WorkshopLoadableItem item in _items ) {
				yield return item.LoadFile( directory, item.SourceFileName );

				if( item is WorkshopModel )
					modelPaths.Remove( item.SourceFileName );
				else if( item is WorkshopTexture )
					texturePaths.Remove( item.SourceFileName );
			}

			// Whatever remains is new
			foreach( string path in modelPaths ) {
				Debug.Log(path);
				WorkshopModel model = new WorkshopModel( Guid.NewGuid() );
				model.Name = path;
				foreach( object obj in model.LoadFile( directory, path ) )
					yield return null;
				Debug.Log( "after load");

				_items.Add( model );
			}

			foreach( string path in texturePaths ) {
				WorkshopTexture texture = new WorkshopTexture( Guid.NewGuid() );
				texture.Name = path;
				foreach( object obj in texture.LoadFile( directory, path ) )
					yield return null;

				_items.Add( texture );
			}
		}
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
		public WorkshopModel DisplayModel, CollisionModel;
		private MaterialData _material;

		public WorkshopDecorativeObject( Guid guid ) : base( guid ) {
			_material = new Assets.MaterialData();
		}

		public MaterialData Material
			{ get { return _material; } }
	}

	/*public class WorkshopToken : WorkshopItem {

	}*/

	public class WorkshopTexture : WorkshopLoadableItem {
		byte[] _textureFileData;
		Texture2D _texture;

		public WorkshopTexture( Guid guid ) : base( guid ) {
		}

		protected override void LoadFileImpl( string fileName, Action<Exception> callback ) {
			_textureFileData = null;

			if( _texture != null ) {
				UnityEngine.Object.Destroy( _texture );
				_texture = null;
			}

			WorkshopBundleLoader.ReadWholeFile( fileName, (err, result) => {
				if( err != null ) {
					callback(err);
					return;
				}

				_textureFileData = result;

				try {
					Texture2D mesh = Texture;
				}
				catch( Exception ex ) {
					callback(ex);
					return;
				}

				callback(null);
			} );
		}

		public Texture2D Texture {
			get {
				if( _texture == null && _textureFileData != null ) {
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

	public abstract class WorkshopLoadableItem : WorkshopItem {
		Exception _loadError;
		string _sourceFileName;
		bool _fileMissing;

		protected WorkshopLoadableItem( Guid guid ) : base( guid ) {
		}

		/// <summary>
		/// Gets the load error, if there was one.
		/// </summary>
		public Exception LoadError {
			get { return _loadError; }
		}

		/// <summary>
		/// Gets the last recorded file name that this texture was loaded from.
		/// </summary>
		public string SourceFileName {
			get { return _sourceFileName; }
		}

		public IEnumerable LoadFile( string basePath, string fileName ) {
			_sourceFileName = fileName;
			string fullPath = Path.GetFullPath( Path.Combine( basePath, fileName ) );
			bool done = false;
			Debug.Log("here2");

			LoadFileImpl( fullPath, err => {
				_loadError = err;
				Debug.Log("callback hit");
				done = true;
			} );

			while( !done )
				yield return null;
		}

		protected abstract void LoadFileImpl( string fileName, Action<Exception> callback );
	}

	public class WorkshopModel : WorkshopLoadableItem {
		string _objText;
		Mesh _mesh;

		public WorkshopModel( Guid guid ) : base( guid ) {
		}

		protected override void LoadFileImpl( string fileName, Action<Exception> callback ) {
			_objText = null;

			if( _mesh != null ) {
				UnityEngine.Object.Destroy( _mesh );
				_mesh = null;
			}

			WorkshopBundleLoader.ReadWholeFile( fileName, (err, result) => {
				Debug.Log( err );
				Debug.Log( result );
				if( err != null ) {
					callback(err);
					return;
				}

				_objText = (result != null) ? Encoding.ASCII.GetString( result ) : null;

				try {
					Mesh mesh = Mesh;
				}
				catch( Exception ex ) {
					callback(ex);
					return;
				}

				callback(null);
			} );
		}

		public Mesh Mesh {
			get {
				if( _mesh == null && _objText != null ) {
					_mesh = ObjImporter.ImportFile( _objText );
				}

				return _mesh;
			}
		}
	}

	public struct NVector4 {
		public float X, Y, Z, W;
	}

	public class MaterialData {
		//Diffuse
		public NVector4 diffuseColor;
		public WorkshopTexture diffuseTexture;

		//Specularity
		public NVector4 specularColor;
		public WorkshopTexture specularTexture;
		public float specularHardness;

		//Emmissive
		public NVector4 emissiveColor;
		public WorkshopTexture emissiveTexture;

		//Normal
		public WorkshopTexture normalTexture;

		//Ambient Occlusion
		public WorkshopTexture ambientOcclusionTexture;

		public void UpdateMaterial( Material material ) {
			material.SetColor( "_Color", new Color( diffuseColor.X, diffuseColor.Y, diffuseColor.Z, diffuseColor.W ) );
			material.SetTexture( "_MainTex", (diffuseTexture != null) ? diffuseTexture.Texture : null );
			material.SetColor( "_SpecColor", new Color( emissiveColor.X, emissiveColor.Y, emissiveColor.Z, emissiveColor.W ) );
			material.SetTexture( "_SpecGlossMap", (specularTexture != null) ? specularTexture.Texture : null );
			material.SetFloat( "_SpecularHighlights", specularHardness );
			material.SetColor( "_EmissionColor", new Color( emissiveColor.X, emissiveColor.Y, emissiveColor.Z, emissiveColor.W ) );
			material.SetTexture( "_EmissionMap", (emissiveTexture != null) ? emissiveTexture.Texture : null );
			material.SetTexture( "_BumpMap", (normalTexture != null) ? normalTexture.Texture : null );
			material.SetTexture( "_OcclusionMap", (ambientOcclusionTexture != null) ? ambientOcclusionTexture.Texture : null );
		}
	}
}
